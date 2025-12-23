using Clinica.Api.Domain.Context;
using Clinica.Api.Domain.Entities;
using Clinica.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Api.Services.Implementations
{
    public class DoencaService : IDoencaService 
    {
        private readonly ClinicaContext _context;

        public DoencaService(ClinicaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Doenca>> ListarAsync()
        {
            return await _context.Doencas.AsNoTracking().ToListAsync();
        }

        public async Task<Doenca?> BuscarPorIdAsync(Guid id)
        {
            return await _context.Doencas.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Doenca> CriarAsync(Doenca doenca)
        {
            doenca.Id = Guid.NewGuid();
            _context.Doencas.Add(doenca);
            await _context.SaveChangesAsync();
            return doenca;
        }

    }
}