using System.Text.Json;

namespace HospitalGateway.Tests;

public class OcelotConfigTests
{
    [Fact]
    public void OcelotJson_DeveSerJsonValido()
    {
        var jsonPath = GetOcelotJsonPath();
        var json = File.ReadAllText(jsonPath);

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("Routes", out _));
    }

    [Fact]
    public void OcelotJson_DeveTerRotasMinimasEsperadas()
    {
        var jsonPath = GetOcelotJsonPath();
        var json = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(json);

        var routes = doc.RootElement.GetProperty("Routes");
        Assert.True(routes.GetArrayLength() >= 6);

        var upstreams = routes.EnumerateArray()
            .Select(r => r.GetProperty("UpstreamPathTemplate").GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("/agendamentos", upstreams);
        Assert.Contains("/consultas", upstreams);
        Assert.Contains("/cirurgias", upstreams);
    }

    [Fact]
    public void OcelotJson_RotasDevemTerDownstreamHostEPort()
    {
        var jsonPath = GetOcelotJsonPath();
        var json = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(json);

        var routes = doc.RootElement.GetProperty("Routes");
        foreach (var route in routes.EnumerateArray())
        {
            var hosts = route.GetProperty("DownstreamHostAndPorts");
            Assert.True(hosts.GetArrayLength() >= 1);

            var first = hosts[0];
            Assert.True(first.TryGetProperty("Host", out var hostProp));
            Assert.True(first.TryGetProperty("Port", out var portProp));

            Assert.False(string.IsNullOrWhiteSpace(hostProp.GetString()));
            Assert.True(portProp.GetInt32() > 0);
        }
    }

    [Fact]
    public void OcelotJson_GlobalConfiguration_DeveTerBaseUrl()
    {
        var jsonPath = GetOcelotJsonPath();
        var json = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(json);

        var global = doc.RootElement.GetProperty("GlobalConfiguration");
        var baseUrl = global.GetProperty("BaseUrl").GetString();
        Assert.False(string.IsNullOrWhiteSpace(baseUrl));
    }

    private static string GetOcelotJsonPath()
    {
        // ocelot.json é copiado para output do HospitalGateway.csproj, então dá pra achar
        // pela raiz do repo também, de forma determinística.
        var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
        var path = Path.Combine(repoRoot, "gateway", "HospitalGateway", "ocelot.json");
        Assert.True(File.Exists(path), $"Não encontrei o arquivo: {path}");
        return path;
    }

    private static string FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "hospitalhub.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Não consegui localizar a raiz do repo (hospitalhub.sln)." );
    }
}