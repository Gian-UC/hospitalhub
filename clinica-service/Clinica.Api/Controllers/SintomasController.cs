using Clinica.Api.Domain.Entities;
using Clinica.Api.DTOs;
using Clinica.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SintomasController : ControllerBase
    {
        private readonly ISintomaService _service;

        public SintomasController(ISintomaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> BuscarPorId()
        {
            var sintomas = await _service.ListarAsync();
            return Ok(sintomas);
        }

        [HttpGet("doenca/{doencaId}")]
        public async Task<IActionResult> ListarPorDoenca(Guid doencaId)
        {
            var sintomas = await _service.ListarPorDoencaIdAsync(doencaId);
            return Ok(sintomas);
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] SintomaCreateDto dto)
        {
            var sintoma = new Sintoma
            {
                Nome = dto.Nome,
                Prioridade = dto.Prioridade,
                DoencaId = dto.DoencaId
            };
            
            var criado = await _service.CriarAsync(sintoma);
            return CreatedAtAction(nameof(BuscarPorId), new { id = criado.Id }, criado);
        }
    }
}