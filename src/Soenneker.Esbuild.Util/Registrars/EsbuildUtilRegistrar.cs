using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Esbuild.Util.Abstract;
using Soenneker.Node.Util.Registrars;

namespace Soenneker.Esbuild.Util.Registrars;

/// <summary>Dependency injection registration for esbuild and its Node.js dependency.</summary>
public static class EsbuildUtilRegistrar
{
    /// <summary>Registers esbuild and Node utilities as singletons. Register EsbuildInstallOptions to customize installation.</summary>
    public static IServiceCollection AddEsbuildUtilAsSingleton(this IServiceCollection services)
    {
        services.AddNodeUtilAsSingleton();
        services.TryAddSingleton<IEsbuildUtil, EsbuildUtil>();
        return services;
    }

    /// <summary>Registers esbuild and Node utilities as scoped services. Register EsbuildInstallOptions to customize installation.</summary>
    public static IServiceCollection AddEsbuildUtilAsScoped(this IServiceCollection services)
    {
        services.AddNodeUtilAsScoped();
        services.TryAddScoped<IEsbuildUtil, EsbuildUtil>();
        return services;
    }
}
