using System.Reflection;
using PSuite.Shared.Abstractions.Modules;

namespace PSuite.Bootstrapper;

public static class ModuleLoader
{
    internal static IEnumerable<Assembly> LoadAssemblies(IConfiguration configuration)
    {
        const string moduleConfigPrefix = "PSuite.Modules.";
        //List of all referenced assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        //We have to load manually modules assemblies since they're not auto loaded
        //because of assembly lazy loading (It will load/bind assemblies lazily, only when it needs to call a method or use a type from that assembly.)
        //https://stackoverflow.com/questions/2384592/is-there-a-way-to-force-all-referenced-assemblies-to-be-loaded-into-the-app-doma
        //https://michaelscodingspot.com/assemblies-load-in-dotnet/
        var alreadyLoadedAssembliesLocations = assemblies
            .Where(x => !x.IsDynamic)
            .Select(x => x.Location)
            .ToArray();

        var notLoadedFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll")
            .Where(x => !alreadyLoadedAssembliesLocations.Contains(x, StringComparer.InvariantCulture))
            .ToList();

        var disabledModulesFiles= new List<string>();
        foreach(var file in notLoadedFiles)
        {
            if(!file.Contains(moduleConfigPrefix))
            {   
                continue;
            }

            var moduleName = file.Split(moduleConfigPrefix, StringSplitOptions.None)[1].Split('.')[0];
            var isModuleEnabled = configuration.GetSection($"{moduleName}:module").GetValue<bool>("enabled");
            if(!isModuleEnabled)
            {
                disabledModulesFiles.Add(file);
            }
        }

        foreach(var fileToDelete in disabledModulesFiles)
        {
            notLoadedFiles.Remove(fileToDelete);
        }

        foreach(var file in notLoadedFiles)
        {
            //add missing assemblies to app domain
            var assembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(file));
            assemblies.Add(assembly);
        }

        return assemblies;
    }

    internal static IEnumerable<IModule> LoadModules(IEnumerable<Assembly> assemblies)
    {
        var modules = assemblies
            .SelectMany(x => x.GetTypes())
            .Where(x => typeof(IModule).IsAssignableFrom(x) && typeof(IModule) != x)
            .Select(Activator.CreateInstance)
            .Cast<IModule>()
            .ToList();

        return modules;
    }
}
