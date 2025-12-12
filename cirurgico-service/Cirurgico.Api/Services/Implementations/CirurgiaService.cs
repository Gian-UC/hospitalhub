using Cirurgico.Api.Domain.Context;
using Cirurgico.Api.Domain.Entities;
using Cirurgico.Api.Messaging.Events;
using Cirurgico.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cirurgico.Api.Services.Implementations
{
    public class CirurgiaService : ICirurgiaService
    {
        private readonly CirurgicoContext _context;

        public CirurgiaService(CirurgicoContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Cirurgia>> ListarAsync()
        {
            return await _context.Cirurgias.ToListAsync();
        }

        public async Task<Cirurgia?> BuscarPorIdAsync(Guid id)
        {
            return await _context.Cirurgias.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Cirurgia> RegistrarCirurgiaAsync(Cirurgia cirurgia)
        {
            cirurgia.Id = Guid.NewGuid();
            cirurgia.CriadoEm = DateTime.UtcNow;

            _context.Cirurgias.Add(cirurgia);
            await _context.SaveChangesAsync();

            return cirurgia;
        }

        public async Task<bool> AtualizarStatusAsync(Guid id, CirurgiaStatus novoStatus)
        {
            var cirurgia = await _context.Cirurgias.FindAsync(id);

            if (cirurgia == null)
                return false;

            cirurgia.Status = novoStatus;
            await _context.SaveChangesAsync();

            return true;
        }

        // ❤️ A MÁGICA ACONTECE AQUI
        public async Task RegistrarCirurgiaPorAgendamentoAsync(AgendamentoConfirmadoEvent evt)
        {
            var cirurgia = new Cirurgia
            {
                Id = Guid.NewGuid(),
                AgendamentoId = evt.AgendamentoId,
                PacienteId = evt.PacienteId,
                DataHora = evt.DataHora,
                Tipo = evt.Tipo,
                Status = CirurgiaStatus.Pendente,
                CriadoEm = DateTime.UtcNow
            };

            _context.Cirurgias.Add(cirurgia);
            await _context.SaveChangesAsync();
        }
    }
}
