using Clinica.Api.Domain.Context;
using Clinica.Api.Domain.Entities;
using Clinica.Api.Domain.Enums;
using Clinica.Api.DTOs;
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

        
        public async Task<Consulta> RegistrarConsultaAsync(Consulta consulta)
        {
            consulta.Id = Guid.NewGuid();
            consulta.CriadoEm = DateTime.UtcNow;
            consulta.Status = StatusConsulta.Pendente;

            _context.Consultas.Add(consulta);
            await _context.SaveChangesAsync();

            return consulta;
        }

        
        public async Task RegistrarConsultaPorAgendamentoAsync(AgendamentoConfirmadoEvent evt)
        {
            var conflito = await _context.Consultas.AnyAsync(c => c.DataHora == evt.DataHora && c.Tipo == evt.Tipo && c.Status != StatusConsulta.Cancelada);

            if (conflito)
                throw new InvalidOperationException("Já existe uma consulta marcada para este horário.");

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

        public async Task VincularSintomasAsync(Guid consultaId, IEnumerable<Guid> sintomaIds)
        {
            var consulta = await _context.Consultas
                .Include(c => c.Sintomas)
                .FirstOrDefaultAsync(c => c.Id == consultaId);

            if (consulta is null)
                throw new KeyNotFoundException("Consulta Não encontrada");

            var sintomasExistentes = await _context.Sintomas
                .Where(s => sintomaIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            foreach (var sintomaId in sintomasExistentes)
            {
                var jaExiste = consulta.Sintomas.Any(cs => cs.SintomaId == sintomaId);

                if (!jaExiste)
                {
                    consulta.Sintomas.Add(new ConsultaSintoma
                    {
                        ConsultaId = consulta.Id,
                        SintomaId = sintomaId
                    });
                }
            }

            var doencasSugeridas = await _context.ConsultaSintomas
                .Where(cs => cs.ConsultaId == consultaId)
                .Include(cs => cs.Sintoma)
                    .ThenInclude(s => s.Doenca)
                .GroupBy(cs => cs.Sintoma.Doenca)
                .Select(g => new DoencaSugeridaDto
                {
                    DoencaId = g.Key.Id,
                    Nome = g.Key.Nome,
                    QuantidadeSintomas = g.Count(),
                    MaiorPrioridade = g.Max(x => (int)x.Sintoma.Prioridade)
                })
                .OrderByDescending(ds => ds.QuantidadeSintomas)
                .ThenByDescending(ds => ds.MaiorPrioridade)
                .ToListAsync();

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<DoencaSugeridaDto>> ObterDoencasSugeridasAsync(Guid consultaId)
        {
            var sugestoes = await _context.ConsultaSintomas
                .Where(cs => cs.ConsultaId == consultaId)
                .Include(cs => cs.Sintoma)
                    .ThenInclude(s => s.Doenca)
                .GroupBy(cs => cs.Sintoma.Doenca)
                .Select(g => new DoencaSugeridaDto
                {
                    DoencaId = g.Key.Id,
                    Nome = g.Key.Nome,
                    QuantidadeSintomas = g.Count(),
                    MaiorPrioridade = g.Max(x => (int)x.Sintoma.Prioridade)
                })
                .OrderByDescending(d => d.QuantidadeSintomas)
                .ThenByDescending(d => d.MaiorPrioridade)
                .ToListAsync();

            return sugestoes;
        }
    }
}
    
