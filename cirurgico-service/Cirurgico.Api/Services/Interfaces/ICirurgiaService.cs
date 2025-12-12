using Cirurgico.Api.Domain.Entities;
using Cirurgico.Api.Messaging.Events;

namespace Cirurgico.Api.Services.Interfaces
{
    public interface ICirurgiaService
    {
        Task<IEnumerable<Cirurgia>> ListarAsync();
        Task<Cirurgia?> BuscarPorIdAsync(Guid id);
        Task <Cirurgia> RegistrarCirurgiaAsync(Cirurgia cirurgia);
        Task<bool> AtualizarStatusAsync(Guid id, CirurgiaStatus novoStatus);

        Task RegistrarCirurgiaPorAgendamentoAsync(AgendamentoConfirmadoEvent evt);
    }
}