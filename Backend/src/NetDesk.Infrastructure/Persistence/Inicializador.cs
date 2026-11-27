using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NetDesk.Infrastructure.Persistence;

public static class Inicializador
{
    public static async Task AplicarAsync(IServiceProvider servicios)
    {
        using var alcance = servicios.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<NetDeskDbContext>();
        var logger = alcance.ServiceProvider.GetRequiredService<ILogger<NetDeskDbContext>>();

        for (var intento = 1; intento <= 30; intento++)
        {
            try
            {
                await db.Database.MigrateAsync();
                await AplicarScriptsAsync(db);
                await Semilla.AplicarAsync(alcance.ServiceProvider);
                logger.LogInformation("Base de datos NetDesk lista.");
                return;
            }
            catch (Exception ex) when (EsFallaDeConexion(ex) && intento < 30)
            {
                logger.LogWarning(ex, "Alerta: SQL Server aún no responde. Intento {Intento}/30.", intento);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }

    private static async Task AplicarScriptsAsync(NetDeskDbContext db)
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "scripts", "stored-procedures.sql");
        if (!File.Exists(ruta))
        {
            throw new FileNotFoundException("No se encontró el script de procedimientos.", ruta);
        }

        var script = await File.ReadAllTextAsync(ruta);
        var lotes = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var lote in lotes)
        {
            if (string.IsNullOrWhiteSpace(lote))
            {
                continue;
            }

            await db.Database.ExecuteSqlRawAsync(lote);
        }
    }

    private static bool EsFallaDeConexion(Exception ex)
    {
        for (var actual = ex; actual is not null; actual = actual.InnerException)
        {
            if (actual is SqlException or TimeoutException)
            {
                return true;
            }
        }

        return false;
    }
}

public class DisenoDbContextFactory : IDesignTimeDbContextFactory<NetDeskDbContext>
{
    public NetDeskDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<NetDeskDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NetDeskIsp;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True")
            .Options;
        return new NetDeskDbContext(opciones);
    }
}
