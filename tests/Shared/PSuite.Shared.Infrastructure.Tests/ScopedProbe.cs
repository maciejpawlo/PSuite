using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace PSuite.Shared.Infrastructure.Tests;

public sealed class ScopedProbe : IDisposable
{
    public bool Disposed { get; private set; }

    public void Dispose() => Disposed = true;
}

public sealed class Recorder
{
    public bool Handled { get; set; }
    public bool? DisposedDuringHandler { get; set; }
    public ScopedProbe? Probe { get; set; }
    public CancellationToken Token { get; set; }
}

public sealed class TestDomainException : Exception;

internal static class TestProvider
{
    internal static readonly IList<Assembly> Assemblies = [typeof(TestProvider).Assembly];

    internal static ServiceProvider Build(Recorder recorder, Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddScoped<ScopedProbe>();
        configure(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
