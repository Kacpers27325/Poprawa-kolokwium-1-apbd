using Microsoft.AspNetCore.Mvc;
using WebApplication1.DTOs;
using WebApplication1.Repositories;
namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnimalsController : ControllerBase
    {
        private readonly IAnimalsRepository _animalsRepository;
        public AnimalsController(IAnimalsRepository animalsRepository)
        {
            _animalsRepository = animalsRepository;
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAnimal(int id)
        {
            if (!await _animalsRepository.DoesAnimalExist(id))
                return NotFound("Zwierze o podanym id nie istnieje");
            var animal = await _animalsRepository.GetAnimal(id);
            return Ok(animal);
        }
        
        [HttpPost]
        public async Task<IActionResult> AddAnimal(ProceduredAnimal newAnimalWithProcedures)
        {
            if (!await _animalsRepository.DoesOwnerExist(newAnimalWithProcedures.OwnerId))
                return NotFound("Wlasciciel nie istnieje");
            foreach (var procedure in newAnimalWithProcedures.Procedures)
            {
                if (!await _animalsRepository.DoesProcedureExist(procedure.ProcedureId))
                    return NotFound("Bledna procedura");
            }
            await _animalsRepository.AddNewAnimalWithProcedures(newAnimalWithProcedures);
            return Created(Request.Path.Value ?? "api/animals", newAnimalWithProcedures);
        }
    }
}