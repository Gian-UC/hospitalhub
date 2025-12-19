using Agendamentos.Api.DTOs;
using Agendamentos.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Agendamentos.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AgendamentosController : ControllerBase
    {
        private readonly IAgendamentoService _service;

        public AgendamentosController(IAgendamentoService service)
        {
            _service = service;
        }

        // POST api/agendamentos
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] AgendamentoCreateDto dto)
        {
            try
            {
                var result = await _service.CriarAsync(dto);
                return CreatedAtAction(nameof(BuscarPorId), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // GET api/agendamentos
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var result = await _service.ListarAsync();
            return Ok(result);
        }

        // GET api/agendamentos/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarPorId(Guid id)
        {
            var result = await _service.BuscarPorIdAsync(id);
            if (result == null)
                return NotFound();
            
            return Ok(result);
        }

        // PUT api/agendamentos/{id}/confirmar
        [HttpPut("{id}/confirmar")]
        public async Task<IActionResult> Confirmar(Guid id)
        {
            var ok = await _service.ConfirmarAsync(id);
            if (!ok)
                return NotFound();
            
            return NoContent();
        }

        // DELETE api/agendamentos/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancelar(Guid id)
        {
            var ok = await _service.CancelarAsync(id);
            if (!ok)
                return NotFound();
            
            return NoContent();
        }
    }
}