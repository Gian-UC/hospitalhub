namespace Notificacao.Api.Services.Email
{
    public interface IEmailService
    {
        Task EnviarAsync(string para, string assunto, string html, CancellationToken ct);
    }
}