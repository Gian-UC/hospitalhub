using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Notificacao.Api.Services.Email;

namespace Notificacao.Api.Tests;

public class SmtpEmailServiceTests
{
    [Fact]
    public void ResolveFromEmail_UsesConfiguredFromEmail_WhenPresent()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Smtp:FromEmail"] = "from@exemplo.com",
                ["Smtp:User"] = "user@exemplo.com"
            })
            .Build();

        var fromEmail = SmtpEmailService.ResolveFromEmail(config);

        Assert.Equal("from@exemplo.com", fromEmail);
    }

    [Fact]
    public void ResolveFromEmail_FallsBackToUser_WhenFromEmailMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Smtp:User"] = "user@exemplo.com"
            })
            .Build();

        var fromEmail = SmtpEmailService.ResolveFromEmail(config);

        Assert.Equal("user@exemplo.com", fromEmail);
    }

    [Fact]
    public void ResolveFromEmail_FallsBackToDefault_WhenUserAndFromEmailMissing()
    {
        var config = new ConfigurationBuilder().Build();

        var fromEmail = SmtpEmailService.ResolveFromEmail(config);

        Assert.Equal("no-reply@hospital.local", fromEmail);
    }

    [Fact]
    public void ResolveSecureSocketOptions_UsesSslOnConnect_WhenUseSslTrue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Smtp:UseSsl"] = "true",
                ["Smtp:UseStartTls"] = "false"
            })
            .Build();

        var opt = SmtpEmailService.ResolveSecureSocketOptions(config, port: 465);

        Assert.Equal(SecureSocketOptions.SslOnConnect, opt);
    }

    [Fact]
    public void ResolveSecureSocketOptions_UsesStartTls_WhenUseStartTlsTrue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Smtp:UseSsl"] = "false",
                ["Smtp:UseStartTls"] = "true"
            })
            .Build();

        var opt = SmtpEmailService.ResolveSecureSocketOptions(config, port: 587);

        Assert.Equal(SecureSocketOptions.StartTls, opt);
    }

    [Fact]
    public void ResolveSecureSocketOptions_Heuristic_UsesStartTls_ForPort587_WhenUnset()
    {
        var config = new ConfigurationBuilder().Build();

        var opt = SmtpEmailService.ResolveSecureSocketOptions(config, port: 587);

        Assert.Equal(SecureSocketOptions.StartTls, opt);
    }

    [Fact]
    public void ResolveSecureSocketOptions_Heuristic_UsesSslOnConnect_ForPort465_WhenUnset()
    {
        var config = new ConfigurationBuilder().Build();

        var opt = SmtpEmailService.ResolveSecureSocketOptions(config, port: 465);

        Assert.Equal(SecureSocketOptions.SslOnConnect, opt);
    }
}