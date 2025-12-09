using Agendamentos.Api.DTOs;

namespace Agendamentos.Api.Services.Interfaces
{
    public interface IAgendamentoService
    {
        Task<AgendamentoResponseDto> CriarAsync(AgendamentoCreateDto dto);
        Task<IEnumerable<AgendamentoResponseDto>> ListarAsync();
        Task<AgendamentoResponseDto?> BuscarPorIdAsync(Guid id);
        Task<bool> ConfirmarAsync(Guid id);
        Task<bool> CancelarAsync(Guid id);
    }
}