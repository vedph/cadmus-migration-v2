using Cadmus.Export.ML;
using Cadmus.Export.Preview;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;
using System.Text;

namespace Cadmus.Export.Test;

internal static class TestHelper
{
    public static string CS = "mongodb://localhost:27017/cadmus-test";

    public static string LoadResourceText(string name)
    {
        using StreamReader reader = new(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"Cadmus.Export.Test.Assets.{name}")!,
            Encoding.UTF8);
        return reader.ReadToEnd();
    }
    private static IHost GetHost(string config)
    {
        return new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                CadmusPreviewFactory.ConfigureServices(services, new[]
                {
                    typeof(TeiStandoffTextTreeRenderer).Assembly
                });
            })
            // extension method from Fusi library
            .AddInMemoryJson(config)
            .Build();
    }

    public static CadmusPreviewFactory GetFactory()
    {
        return new CadmusPreviewFactory(GetHost(LoadResourceText("Preview.json")))
        {
            ConnectionString = "mongodb://localhost:27017/cadmus-test"
        };
    }
}
