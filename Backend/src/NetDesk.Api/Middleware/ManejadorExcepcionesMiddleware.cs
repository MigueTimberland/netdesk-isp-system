using NetDesk.Application.Comun;
using NetDesk.Domain.Common;

namespace NetDesk.Api.Middleware;

public class ManejadorExcepcionesMiddleware(RequestDelegate siguiente, ILogger<ManejadorExcepcionesMiddleware> logger)
{
    public async Task Invoke(HttpContext contexto)
    {
        try
        {
            await siguiente(contexto);
        }
        catch (Exception ex)
        {
            await EscribirAsync(contexto, ex);
        }
    }

    private async Task EscribirAsync(HttpContext contexto, Exception ex)
    {
        var (estado, mensaje, errores) = ex switch
        {
            ValidacionException validacion => (StatusCodes.Status400BadRequest, validacion.Message, validacion.Errores),
            ReglaNegocioException regla => (StatusCodes.Status400BadRequest, regla.Message, null),
            NoEncontradoException noEncontrado => (StatusCodes.Status404NotFound, noEncontrado.Message, null),
            UnauthorizedAccessException noAutorizado => (StatusCodes.Status401Unauthorized, noAutorizado.Message, null),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.", null)
        };

        if (estado == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}.", contexto.Request.Method, contexto.Request.Path);
        }
        else if (estado == StatusCodes.Status400BadRequest)
        {
            logger.LogWarning("Alerta de negocio en {Ruta}: {Mensaje}", contexto.Request.Path, mensaje);
        }

        if (contexto.Response.HasStarted)
        {
            throw ex;
        }

        contexto.Response.StatusCode = estado;
        contexto.Response.ContentType = "application/json";
        await contexto.Response.WriteAsJsonAsync(new { mensaje, errores });
    }
}
