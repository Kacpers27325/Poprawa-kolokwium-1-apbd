using WebApplication1.DTOs;

namespace WebApplication1.Repositories;


public interface IAnimalsRepository
{
    Task<bool> DoesAnimalExist(int id);
    Task<bool> DoesOwnerExist(int id);
    Task<bool> DoesProcedureExist(int id);
    Task<AnimalDto> GetAnimal(int id);


    Task AddNewAnimalWithProcedures(ProceduredAnimal newAnimalWithProcedures);
}
