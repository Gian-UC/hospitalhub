using Clinica.Api.Domain.Entities;
using Clinica.Api.DTOs;
using Clinica.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultasController : ControllerBase
    {
        private readonly IConsultaService _service;

        public ConsultasController(IConsultaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            return Ok(await _service.ListarAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarPorId(Guid id)
        {
            var result = await _service.BuscarPorIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] ConsultaCreateDto dto)
        {
            var consulta = new Consulta
            {
                AgendamentoId = dto.AgendamentoId,
                PacienteId = dto.PacienteId,
                DataHora = dto.DataHora,
                Tipo = dto.Tipo
            };

            var criada = await _service.RegistrarConsultaAsync(consulta);

            return CreatedAtAction(nameof(BuscarPorId), new { id = criada.Id }, criada);
        }


        [HttpPost("{consultaId}/sintomas")]
        public async Task<IActionResult> VincularSintomas(Guid consultaId, [FromBody] ConsultaSintomasAddDto dto)
        {
            await _service.VincularSintomasAsync(consultaId, dto.SintomaIds);
            return NoContent();
        }
    }
}