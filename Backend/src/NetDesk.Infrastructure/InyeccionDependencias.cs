using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetDesk.Application.Abstracciones;
using NetDesk.Infrastructure.Archivos;
using NetDesk.Infrastructure.Persistence;
using NetDesk.Infrastructure.Seguridad;

namespace NetDesk.Infrastructure;

public static class InyeccionDependencias
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuracion)
    {
        var cadena = configuracion.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Default.");

        services.AddDbContext<NetDeskDbContext>(opciones => opciones.UseSqlServer(cadena));

        services.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        services.AddScoped<ITokenRecuperacionRepositorio, TokenRecuperacionRepositorio>();
        services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
        services.AddScoped<ICajaRepositorio, CajaRepositorio>();
        services.AddScoped<ITurnoRepositorio, TurnoRepositorio>();
        services.AddScoped<IContratoRepositorio, ContratoRepositorio>();
        services.AddScoped<IPagoRepositorio, PagoRepositorio>();
        services.AddScoped<ICatalogoRepositorio, CatalogoRepositorio>();
        services.AddScoped<IDashboardRepositorio, DashboardRepositorio>();
        services.AddSingleton<ICifradoService, CifradoService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<TokenJwtService>();
        services.AddSingleton<ITokenJwtService>(sp => sp.GetRequiredService<TokenJwtService>());
        services.AddScoped<IUsuarioActual, UsuarioActual>();
        services.AddSingleton<IReloj, Reloj>();
        services.AddSingleton<IOpcionesAplicacion, OpcionesAplicacion>();
        services.AddSingleton<ILectorArchivoMasivo, LectorArchivoMasivo>();
        return services;
    }
}
