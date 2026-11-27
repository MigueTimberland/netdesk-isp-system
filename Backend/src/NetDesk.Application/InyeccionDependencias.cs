using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NetDesk.Application.Servicios;
using NetDesk.Application.Validacion;

namespace NetDesk.Application;

public static class InyeccionDependencias
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        services.AddScoped<Mapeador>();
        services.AddScoped<AuthService>();
        services.AddScoped<UsuarioService>();
        services.AddScoped<ClienteService>();
        services.AddScoped<CajaService>();
        services.AddScoped<TurnoService>();
        services.AddScoped<ContratoService>();
        services.AddScoped<CatalogoService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}
