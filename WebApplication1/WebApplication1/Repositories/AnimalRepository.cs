using System.Data.SqlClient;
using WebApplication1.DTOs;

 
namespace WebApplication1.Repositories
{
    public class AnimalsRepository : IAnimalsRepository
    {
        private readonly IConfiguration _configuration;
        public AnimalsRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<bool> DoesAnimalExist(int id)
        {
            var query = "SELECT 1 FROM Animal WHERE ID = @ID";
            await using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ID", id);
            await connection.OpenAsync();
            var res = await command.ExecuteScalarAsync();
            return res != null;
        }
        public async Task<bool> DoesOwnerExist(int id)
        {
            var query = "SELECT 1 FROM Owner WHERE ID = @ID";
            await using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ID", id);
            await connection.OpenAsync();
            var res = await command.ExecuteScalarAsync();
            return res != null;
        }
        public async Task<bool> DoesProcedureExist(int id)
        {
            var query = "SELECT 1 FROM [Procedure] WHERE ID = @ID";
            await using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ID", id);
            await connection.OpenAsync();
            var res = await command.ExecuteScalarAsync();
            return res != null;
        }
        
        
public async Task<AnimalDto> GetAnimal(int id)
{
    if (!await DoesAnimalExist(id))
    {
        throw new Exception("Animal not found.");
    }
 
    var query = @"
        SELECT 
            Animal.ID AS AnimalID,
            Animal.Name AS AnimalName,
            Animal.AdmissionDate AS AdmissionDate,
            Owner.ID as OwnerID,
            Owner.FirstName AS FirstName,
            Owner.LastName AS LastName,
            Procedure_Animal.Date AS Date,
            [Procedure].Name AS ProcedureName,
            [Procedure].Description AS Description
        FROM Animal
        JOIN Owner ON Owner.ID = Animal.OwnerID
        LEFT JOIN Procedure_Animal ON Procedure_Animal.AnimalID = Animal.ID
        LEFT JOIN [Procedure] ON [Procedure].ID = Procedure_Animal.ProcedureID
        WHERE Animal.ID = @ID";
 
    AnimalDto animalDto = null;
 
    await using (var connection = new SqlConnection(_configuration.GetConnectionString("Default")))
    {
        await using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@ID", id);
            await connection.OpenAsync();
 
            var reader = await command.ExecuteReaderAsync();
 
            if (!reader.HasRows)
            {
                throw new Exception("Animal data not found.");
            }
 
            var animalIdOrdinal = reader.GetOrdinal("AnimalID");
            var animalNameOrdinal = reader.GetOrdinal("AnimalName");
            var admissionDateOrdinal = reader.GetOrdinal("AdmissionDate");
            var ownerIdOrdinal = reader.GetOrdinal("OwnerID");
            var firstNameOrdinal = reader.GetOrdinal("FirstName");
            var lastNameOrdinal = reader.GetOrdinal("LastName");
            var dateOrdinal = reader.GetOrdinal("Date");
            var procedureNameOrdinal = reader.GetOrdinal("ProcedureName");
            var procedureDescriptionOrdinal = reader.GetOrdinal("Description");
 
            while (await reader.ReadAsync())
            {
                if (animalDto == null)
                {
                    animalDto = new AnimalDto()
                    {
                        Id = reader.GetInt32(animalIdOrdinal),
                        Name = reader.GetString(animalNameOrdinal),
                        AdmissionDate = await SafeGetDateTimeAsync(reader, admissionDateOrdinal),
                        Owner = new OwnerDto()
                        {
                            Id = reader.GetInt32(ownerIdOrdinal),
                            FirstName = reader.GetString(firstNameOrdinal),
                            LastName = reader.GetString(lastNameOrdinal)
                        },
                        Procedures = new List<ProcedureDto>()
                    };
                }
 
                if (!reader.IsDBNull(dateOrdinal))
                {
                    animalDto.Procedures.Add(new ProcedureDto()
                    {
                        Date = reader.GetDateTime(dateOrdinal),
                        Name = reader.GetString(procedureNameOrdinal),
                        Description = reader.GetString(procedureDescriptionOrdinal)
                    });
                }
            }
        }
    }
 
    if (animalDto == null)
    {
        throw new Exception("Brak zwierzaka w bazie");
    }
 
    return animalDto;
}
 
private async Task<DateTime> SafeGetDateTimeAsync(SqlDataReader reader, int ordinal)
{
    return await reader.IsDBNullAsync(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
}
        
        
        public async Task AddNewAnimalWithProcedures(ProceduredAnimal newAnimalWithProcedures)
        {
            var insertAnimalQuery = @"INSERT INTO Animal (Name, AdmissionDate, OwnerId, AnimalClassID) 
                              VALUES(@Name, @AdmissionDate, @OwnerId,1);
                              SELECT CAST(SCOPE_IDENTITY() as int);";
            await using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            await using var command = new SqlCommand(insertAnimalQuery, connection);
            command.Parameters.AddWithValue("@Name", newAnimalWithProcedures.Name);
            command.Parameters.AddWithValue("@AdmissionDate", newAnimalWithProcedures.AdmissionDate);
            command.Parameters.AddWithValue("@OwnerId", newAnimalWithProcedures.OwnerId);
 
            await connection.OpenAsync();
            var transaction = await connection.BeginTransactionAsync();
            command.Transaction = transaction as SqlTransaction;
 
            try
            {
                var animalId = (int)await command.ExecuteScalarAsync(); 
 
                var insertProcedureAnimalQuery = "INSERT INTO Procedure_Animal (ProcedureId, AnimalId, Date ) VALUES(@ProcedureId, @AnimalId, @Date)";
                foreach (var procedure in newAnimalWithProcedures.Procedures)
                {
                    using (var procedureCommand = new SqlCommand(insertProcedureAnimalQuery, connection, transaction as SqlTransaction))
                    {
                        procedureCommand.Parameters.AddWithValue("@ProcedureId", procedure.ProcedureId);
                        procedureCommand.Parameters.AddWithValue("@AnimalId", animalId);
                        procedureCommand.Parameters.AddWithValue("@Date", procedure.Date);
                        await procedureCommand.ExecuteNonQueryAsync();
                    }
                }
 
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<int> AddAnimal(NewAnimalDTO animal)
        {
            var insert = @"INSERT INTO Animal VALUES(@Name,  @AdmissionDate, @OwnerId);
                           SELECT @@IDENTITY AS ID;";
            await using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            await using var command = new SqlCommand(insert, connection);
            command.Parameters.AddWithValue("@Name", animal.Name);
            command.Parameters.AddWithValue("@AdmissionDate", animal.AdmissionDate);
            command.Parameters.AddWithValue("@OwnerId", animal.OwnerId);
            await connection.OpenAsync();
            var id = await command.ExecuteScalarAsync();
            if (id == null)
                throw new Exception("Insert failed.");
            return Convert.ToInt32(id);
        }
        public async Task AddProcedureAnimal(int animalId, ProcedureWithDate procedure)
        {
            var query = "INSERT INTO Procedure_Animal VALUES(@ProcedureID, @AnimalID, @Date)";
            await using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProcedureID", procedure.ProcedureId);
            command.Parameters.AddWithValue("@AnimalID", animalId);
            command.Parameters.AddWithValue("@Date", procedure.Date);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
}