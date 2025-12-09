using Agendamentos.Api.DTOs;
using Agendamentos.Api.Domain.Context;
using Agendamentos.Api.Domain.Entities;
using Agendamentos.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Agendamentos.Api.Services.Implementations
{
    public class AgendamentoService : IAgendamentoService
    {
        private readonly HospitalAgendamentosContext _context;

        public AgendamentoService(HospitalAgendamentosContext context)
        {
            _context = context;
        }

        public async Task<AgendamentoResponseDto> CriarAsync(AgendamentoCreateDto dto)
        {
            var paciente = await _context.Pacientes.FindAsync(dto.PacienteId);
            if (paciente == null)
                throw new KeyNotFoundException("Paciente não encontrado.");

            var ag = new Agendamento
            {
                Id = Guid.NewGuid(),
                PacienteId = dto.PacienteId,
                DataHora = dto.DataHora,
                Tipo = (TipoAgendamento)dto.Tipo,
                Descricao = dto.Descricao,
                Confirmado = false,
                DataCriacao = DateTime.UtcNow
            };

            _context.Agendamentos.Add(ag);
            await _context.SaveChangesAsync();

            return await MapToResponseDto(ag);
        }

        public async Task<IEnumerable<AgendamentoResponseDto>> ListarAsync()
        {
            var lista = await _context.Agendamentos
                .Include(a => a.Paciente)
                .ToListAsync();

            var result = new List<AgendamentoResponseDto>();

            foreach (var ag in lista)
                result.Add(await MapToResponseDto(ag));
            return result;
        }

        public async Task<AgendamentoResponseDto?> BuscarPorIdAsync(Guid id)
        {
            var ag = await _context.Agendamentos
                .Include(a => a.Paciente)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (ag == null)
                return null;

            return await MapToResponseDto(ag);
        }

        public async Task<bool> ConfirmarAsync(Guid id)
        {
            var ag = await _context.Agendamentos.FindAsync(id);
            if (ag == null)
                return false;

            ag.Confirmado = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelarAsync(Guid id)
        {
            var ag = await _context.Agendamentos.FindAsync(id);
            if (ag == null)
                return false;

            _context.Agendamentos.Remove(ag);
            await _context.SaveChangesAsync();
            return true;
        }

        private Task<AgendamentoResponseDto> MapToResponseDto(Agendamento ag)
        {
            return Task.FromResult(new AgendamentoResponseDto
            {
                Id = ag.Id,
                Paciente = new PacienteDto
                {
                    Id = ag.Paciente!.Id,
                    Nome = ag.Paciente.Nome,
                    Documento = ag.Paciente.Documento,
                    DataNascimento = ag.Paciente.DataNascimento,
                    Telefone = ag.Paciente.Telefone,
                    Email = ag.Paciente.Email
                },
                DataHora = ag.DataHora,
                Tipo = (int)ag.Tipo,
                Descricao = ag.Descricao,
                Confirmado = ag.Confirmado,
                DataCriacao = ag.DataCriacao
            });
        }
    }
}