using Clinica.Api.Domain.Entities;
using Clinica.Api.Messaging.Events;
using Clinica.Api.DTOs;

namespace Clinica.Api.Services.Interfaces
{
    public interface IConsultaService
    {
        Task<IEnumerable<Consulta>> ListarAsync();
        Task<Consulta?> BuscarPorIdAsync(Guid id);        
        Task<Consulta> RegistrarConsultaAsync(Consulta consulta);        
        Task RegistrarConsultaPorAgendamentoAsync(AgendamentoConfirmadoEvent evt);
        Task VincularSintomasAsync(Guid consultaId, IEnumerable<Guid> sintomaIds);
        Task<IEnumerable<DoencaSugeridaDto>> ObterDoencasSugeridasAsync(Guid consultaId);
    }
}