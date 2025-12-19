using Cirurgico.Api.Domain.Entities;
using Cirurgico.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cirurgico.Api.Controllers
{
    [Authorize(Policy = "MedicoOnly")]
    [ApiController]
    [Route("api/[controller]")]
    public class CirurgiasController : ControllerBase
    {
        private readonly ICirurgiaService _service;
        public CirurgiasController(ICirurgiaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var cirurgias = await _service.ListarAsync();
            return Ok(cirurgias);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Buscar(Guid id)
        {
            var cirurgia = await _service.BuscarPorIdAsync(id);

            if (cirurgia == null)
                return NotFound();

            return Ok(cirurgia);    
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] Cirurgia cirurgia)
        {
            var criada = await _service.RegistrarCirurgiaAsync(cirurgia);
            return CreatedAtAction(nameof(Buscar), new { id = criada.Id }, criada);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] CirurgiaStatus status)
        {
            var ok = await _service.AtualizarStatusAsync(id, status);
            if (!ok)
                return NotFound();

            return NoContent();
        }        
    }
}