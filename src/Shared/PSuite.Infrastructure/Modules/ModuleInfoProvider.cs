using PSuite.Shared.Abstractions.Modules;

namespace PSuite.Shared.Infrastructure.Modules;

internal class ModuleInfoProvider(IEnumerable<IModule> modules)
{
    public IList<IModule> Modules { get; set; } = modules.ToList();
}
