using Clinica.Api.Domain.Context;
using Clinica.Api.Domain.Entities;
using Clinica.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Api.Services.Implementations
{
    public class SintomaService : ISintomaService
    {
        private readonly ClinicaContext _context;

        public SintomaService(ClinicaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Sintoma>> ListarAsync()
        {
            return await _context.Sintomas.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Sintoma>> ListarPorDoencaIdAsync(Guid doencaId)
        {
            return await _context.Sintomas
                .AsNoTracking()
                .Where(s => s.DoencaId == doencaId)
                .ToListAsync();
        }

        public async Task<Sintoma> CriarAsync(Sintoma sintoma)
        {
            sintoma.Id = Guid.NewGuid();
            _context.Sintomas.Add(sintoma);
            await _context.SaveChangesAsync();
            return sintoma;
        }
    }
}