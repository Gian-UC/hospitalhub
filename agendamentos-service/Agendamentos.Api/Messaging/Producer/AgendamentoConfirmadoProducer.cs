using System.Text;
using System.Text.Json;
using Agendamentos.Api.Messaging.Events;
using RabbitMQ.Client;

namespace Agendamentos.Api.Messaging.Producer
{
    public class AgendamentoConfirmadoProducer
    {
        private const string ExchangeName = "agendamentos.events";

        private readonly IConnection _connection;
        private readonly IModel _channel;

        public AgendamentoConfirmadoProducer()
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
        }

        public void Publicar(AgendamentoConfirmadoEvent evt)
        {
            var json = JsonSerializer.Serialize(evt);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: string.Empty,
                basicProperties: null,
                body: body
            );
        }
    }
}
