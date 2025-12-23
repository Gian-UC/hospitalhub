namespace Notificacao.Api.Services.Pacientes
{
    public interface IPacienteCliente
    {
        Task<PacienteDto?> BuscarPorIdAsync(Guid pacienteId, CancellationToken ct);
    }
}