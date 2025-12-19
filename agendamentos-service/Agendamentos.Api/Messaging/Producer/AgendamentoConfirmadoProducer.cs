using System.Text;
using System.Text.Json;
using Agendamentos.Api.Messaging.Events;
using RabbitMQ.Client;

namespace Agendamentos.Api.Messaging.Producer
{
    public class AgendamentoConfirmadoProducer : IAgendamentoConfirmadoProducer
    {
        private const string ExchangeName = "agendamentos.events";

        private IConnection? _connection;
        private IModel? _channel;
        private readonly ILogger<AgendamentoConfirmadoProducer> _logger;

        public AgendamentoConfirmadoProducer(ILogger<AgendamentoConfirmadoProducer> logger)
        {
            _logger = logger;
            TentarConectar();
        }

        private void TentarConectar()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = "rabbitmq",
                    Port = 5672,
                    UserName = "guest",
                    Password = "guest"
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare(
                    exchange: ExchangeName,
                    type: ExchangeType.Fanout,
                    durable: false,
                    autoDelete: false,
                    arguments: null
                );

                _logger.LogInformation("Conectado ao RabbitMQ e exchange '{ExchangeName}' declarada", ExchangeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar ao RabbitMQ. Producer não funcionará corretamente.");
            }
        }

        public void Publicar(AgendamentoConfirmadoEvent evt)
        {
            if (_channel == null || _connection == null || !_connection.IsOpen)
            {
                _logger.LogWarning("Canal RabbitMQ não está disponível. Tentando reconectar...");
                TentarConectar();
            }

            if (_channel == null)
            {
                _logger.LogError("Não foi possível publicar evento. Canal RabbitMQ indisponível.");
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(evt);
                var body = Encoding.UTF8.GetBytes(json);

                _channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: string.Empty,
                    basicProperties: null,
                    body: body
                );

                _logger.LogInformation("Evento AgendamentoConfirmado publicado: {AgendamentoId}", evt.AgendamentoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar evento no RabbitMQ");
            }
        }
    }
}
