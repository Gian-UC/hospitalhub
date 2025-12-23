using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;

namespace Notificacao.Api.Services.Email
{
    public class SmtpEmailService(IConfiguration config) : IEmailService
    {
        internal static string ResolveFromEmail(IConfiguration config)
        {
            var configured = config["Smtp:FromEmail"];
            if (!string.IsNullOrWhiteSpace(configured)) return configured;

            var user = config["Smtp:User"];
            if (!string.IsNullOrWhiteSpace(user)) return user;

            return "no-reply@hospital.local";
        }

        internal static SecureSocketOptions ResolveSecureSocketOptions(IConfiguration config, int port)
        {
            var useSslRaw = config["Smtp:UseSsl"];
            var useStartTlsRaw = config["Smtp:UseStartTls"];

            var useSsl = bool.TryParse(useSslRaw, out var ssl) && ssl;
            var useStartTls = bool.TryParse(useStartTlsRaw, out var startTls) && startTls;

            if (!useSsl && !useStartTls && string.IsNullOrWhiteSpace(useSslRaw) && string.IsNullOrWhiteSpace(useStartTlsRaw))
            {
                if (port == 587) useStartTls = true;
                else if (port == 465) useSsl = true;
            }

            return useSsl
                ? SecureSocketOptions.SslOnConnect
                : (useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
        }

        public async Task EnviarAsync(string para, string assunto, string html, CancellationToken ct)
        {
            var host = config["Smtp:Host"]!;
            var port = int.Parse(config["Smtp:Port"]!);
            var fromName = config["Smtp:FromName"] ?? "Hospital";
            var user = config["Smtp:User"];
            var pass = config["Smtp:Pass"];            

            var fromEmail = ResolveFromEmail(config);
            var secureSocketOptions = ResolveSecureSocketOptions(config, port);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(para));
            message.Subject = assunto;
            message.Body = new TextPart("html") { Text = html};            

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(host, port, secureSocketOptions, ct);

            if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
            {
                var supportsAuth = smtp.Capabilities.HasFlag(SmtpCapabilities.Authentication);
                if (supportsAuth && !smtp.IsAuthenticated)
                    await smtp.AuthenticateAsync(user, pass, ct);
            }

            await smtp.SendAsync(message, ct);
            await smtp.DisconnectAsync(true, ct);
        }
       
    }
}