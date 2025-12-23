using Agendamentos.Api.Messaging.Events;

namespace Agendamentos.Api.Messaging.Producer
{
    public interface IAgendamentoConfirmadoProducer
    {
        void Publicar(AgendamentoConfirmadoEvent evento);
    }
}
