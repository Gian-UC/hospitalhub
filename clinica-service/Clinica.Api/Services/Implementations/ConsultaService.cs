using Clinica.Api.Domain.Context;
using Clinica.Api.Domain.Entities;
using Clinica.Api.Domain.Enums;
using Clinica.Api.Messaging.Events;
using Clinica.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Api.Services.Implementations
{
    public class ConsultaService : IConsultaService
    {
        private readonly ClinicaContext _context;
        public ConsultaService(ClinicaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Consulta>> ListarAsync()
        {
            return await _context.Consultas.ToListAsync();
        }

        public async Task<Consulta?> BuscarPorIdAsync(Guid id)
        {
            return await _context.Consultas.FirstOrDefaultAsync(c => c.Id == id);                
        }

        // Criar uma nova consulta manual (teste)
        public async Task<Consulta> RegistrarConsultaAsync(Consulta consulta)
        {
            consulta.Id = Guid.NewGuid();
            consulta.CriadoEm = DateTime.UtcNow;
            consulta.Status = StatusConsulta.Pendente;

            _context.Consultas.Add(consulta);
            await _context.SaveChangesAsync();

            return consulta;
        }

        // Evente-Driven - Criar consulta a partir do evento de agendamento confirmado
        public async Task RegistrarConsultaPorAgendamentoAsync(AgendamentoConfirmadoEvent evt)
        {
            var consulta = new Consulta
            {
                Id = Guid.NewGuid(),
                AgendamentoId = evt.AgendamentoId,
                PacienteId = evt.PacienteId,
                DataHora = evt.DataHora,
                Tipo = evt.Tipo,
                Status = StatusConsulta.Pendente,
                CriadoEm = DateTime.UtcNow
            };
            
            _context.Consultas.Add(consulta);
            await _context.SaveChangesAsync();
        }
    }
}