using Clinica.Api.Domain.Entities;

namespace Clinica.Api.Services.Interfaces
{
    public interface IDoencaService
    {
        Task<IEnumerable<Doenca>> ListarAsync();
        Task<Doenca?> BuscarPorIdAsync(Guid id);
        Task<Doenca> CriarAsync(Doenca doenca);
    }
}