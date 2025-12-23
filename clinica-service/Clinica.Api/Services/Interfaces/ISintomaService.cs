using Clinica.Api.Domain.Entities;

namespace Clinica.Api.Services.Interfaces
{
    public interface ISintomaService
    {
        Task<IEnumerable<Sintoma>> ListarAsync();
        Task<IEnumerable<Sintoma>> ListarPorDoencaIdAsync(Guid doencaId);
        Task<Sintoma> CriarAsync(Sintoma sintoma);
    }
}