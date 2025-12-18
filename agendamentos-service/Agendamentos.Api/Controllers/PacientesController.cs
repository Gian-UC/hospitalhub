using Agendamentos.Api.Domain.Context;
using Agendamentos.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agendamentos.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PacientesController : ControllerBase
    {
        private readonly HospitalAgendamentosContext _context;

        public PacientesController(HospitalAgendamentosContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] Paciente paciente)
        {
            try
            {
                var cpfJaExiste = await _context.Pacientes.AnyAsync(p => p.Documento == paciente.Documento);

                if (cpfJaExiste)
                    return Conflict(new { mensagem = "Já existe um paciente cadastrado com este CPF." });

                paciente.Id = Guid.NewGuid();

                _context.Pacientes.Add(paciente);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(BuscarPorId), new { id = paciente.Id }, paciente);
            }
            catch (DbUpdateException)
            {
                return Conflict(new { mensagem = "Já existe um paciente cadastrado com este CPF."});
            }
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var pacientes = await _context.Pacientes.ToListAsync();
            return Ok(pacientes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarPorId(Guid id)
        {
            var paciente = await _context.Pacientes.FindAsync(id);

            if (paciente == null)
                return NotFound();

            return Ok(paciente);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Deletar(Guid id)
        {
            var paciente = await _context.Pacientes
                .Include(p => p.Agendamentos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paciente == null)
                return NotFound();

            if (paciente.Agendamentos.Any())
                _context.Agendamentos.RemoveRange(paciente.Agendamentos);

            _context.Pacientes.Remove(paciente);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
