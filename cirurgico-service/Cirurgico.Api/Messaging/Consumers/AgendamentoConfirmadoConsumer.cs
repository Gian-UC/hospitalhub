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
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: "agendamento_confirmado",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

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
                queue: "agendamento_confirmado",
                autoAck: true,
                consumer: consumer
            );

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }
}
