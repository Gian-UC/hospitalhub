using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Notificacao.Api.Messaging.Contracts;
using Notificacao.Api.Services.Email;
using Notificacao.Api.Services.Pacientes;

namespace Notificacao.Api.Messaging
{
    public class AgendamentoConfirmadoConsumer( IConfiguration config, IServiceScopeFactory scopeFactory, ILogger<AgendamentoConfirmadoConsumer> logger) : BackgroundService
    {
        private IConnection? _connection;
        private IModel? _channel;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMQ:Host"] ?? "rabbitmq",
                Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
                UserName = config["RabbitMQ:User"] ?? "guest",
                Password = config["RabbitMQ:Pass"] ?? "guest",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            var exchange = config["RabbitMQ:Exchange"] ?? "agendamentos.events";
            _channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout, durable: false, autoDelete: false);

            var queue = config["RabbitMQ:Queue"] ?? "notificacao.agendamento_confirmado";
            _channel.QueueDeclare(queue: queue, durable: false, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: queue, exchange: exchange, routingKey: "");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var evt = JsonSerializer.Deserialize<AgendamentoConfirmadoEvent>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (evt is null)
                    {
                        logger.LogWarning("[NOTIFICACAO] Evento inválido (null).");
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    using var scope = scopeFactory.CreateScope();
                    var pacienteClient = scope.ServiceProvider.GetRequiredService<IPacienteCliente>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    var paciente = await pacienteClient.BuscarPorIdAsync(evt.PacienteId, stoppingToken);

                    if (paciente is null || string.IsNullOrWhiteSpace(paciente.Email))
                    {
                        logger.LogWarning("[NOTIFICACAO] Paciente não encontrado ou sem e-mail. PacienteId={PacienteId}", evt.PacienteId);
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    var assunto = "✅ Agendamento Confirmado";
                    var corpo = $@"
                        <h2>Agendamento Confirmado</h2>
                        <p>Olá, <b>{paciente.Nome}!</b> 💙</p>
                        <p>Seu agendamento foi confirmado!</p>
                        <ul>
                            <li><b>Data/Hora:</b> {evt.DataHora:dd/MM/yyyy HH:mm}</li>
                            <li><b>Tipo:</b> {evt.Tipo}</li>
                            <li><b>Descrição:</b> {evt.Descricao}</li>
                            <li><b>Emergencial:</b> {(evt.Emergencial ? "Sim" : "Não")}</li>
                        </ul>
                        <p>HospitalHub</p>
                    ";

                    await emailService.EnviarAsync(paciente.Email, assunto, corpo, stoppingToken);
                    logger.LogInformation("[NOTIFICACAO] Email enviado para {Email} (AgendamentoId={AgendamentoId})", paciente.Email, evt.Id);

                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[NOTIFICACAO] Erro processando evento.");
                }
            };

            _channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);
            logger.LogInformation("[NOTIFICACAO] Consumidor iniciado e aguardando eventos.");
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}