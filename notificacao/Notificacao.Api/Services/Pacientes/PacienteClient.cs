using System.Net.Http.Json;

namespace Notificacao.Api.Services.Pacientes
{
    public class PacienteClient(HttpClient http) : IPacienteCliente
    {
        public async Task<PacienteDto?> BuscarPorIdAsync(Guid pacienteId, CancellationToken ct)
        {
            var resp = await http.GetAsync($"/api/Pacientes/{pacienteId}", ct);
            if (!resp.IsSuccessStatusCode) return null;

            return await resp.Content.ReadFromJsonAsync<PacienteDto?>(cancellationToken: ct);
        }
    }
}