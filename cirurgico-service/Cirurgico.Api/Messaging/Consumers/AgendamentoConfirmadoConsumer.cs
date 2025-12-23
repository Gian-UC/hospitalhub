using System.Text;
using System.Text.Json;
using Cirurgico.Api.Messaging.Events;
using Cirurgico.Api.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Cirurgico.Api.Messaging.Consumers
{
    public class AgendamentoConfirmadoConsumer : BackgroundService
    {
        private const string ExchangeName = "agendamentos.events";
        private const string QueueName = "agendamento_confirmado.cirurgico";

        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public AgendamentoConfirmadoConsumer(IServiceProvider provider)
        {
            _serviceProvider = provider;

            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                DispatchConsumersAsync = true
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

            _channel.QueueDeclare(
                queue: QueueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: string.Empty
            );
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var evt = JsonSerializer.Deserialize<AgendamentoConfirmadoEvent>(json);

                Console.WriteLine($"[CIRÚRGICO] Evento Recebido: {json}");

                    if (evt == null) return;

                    using var scope = _serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ICirurgiaService>();

                    await service.RegistrarCirurgiaPorAgendamentoAsync(evt);
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: true,
                consumer: consumer
            );

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }
}
