﻿using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PSuite.Shared.Abstractions.Modules;
using PSuite.Shared.Infrastructure.Modules;

[assembly: InternalsVisibleTo("PSuite.Bootstrapper")]
namespace PSuite.Shared.Infrastructure;
internal static class Extensions
{
    internal static IServiceCollection AddInfrastructure(this IServiceCollection services, 
        IEnumerable<IModule> modules, IEnumerable<Assembly> assemblies)
    {
        services.AddModuleInfo(modules);
        return services;
    }

    internal static void UseInfrastructure(this IApplicationBuilder app)
    {
        
    }
}