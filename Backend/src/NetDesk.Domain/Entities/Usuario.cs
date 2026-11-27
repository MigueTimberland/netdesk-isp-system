using NetDesk.Domain.Common;
using NetDesk.Domain.Enums;

namespace NetDesk.Domain.Entities;

public class Usuario : EntidadAuditable
{
    public const int IntentosMaximos = 3;

    public string NombreUsuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string IdentificacionCifrada { get; set; } = string.Empty;
    public string IdentificacionIndice { get; set; } = string.Empty;
    public string CorreoCifrado { get; set; } = string.Empty;
    public string CorreoIndice { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    public EstadoUsuario Estado { get; set; }
    public bool SesionActiva { get; set; }
    public int IntentosFallidos { get; set; }
    public int? CreadoPorId { get; set; }
    public int? AprobadoPorId { get; set; }
    public DateTime? FechaAprobacion { get; set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();

    public void ValidarPuedeIngresar()
    {
        if (Eliminado)
        {
            throw new ReglaNegocioException("El usuario no existe.");
        }

        if (Estado == EstadoUsuario.Bloqueado)
        {
            throw new ReglaNegocioException("El usuario está bloqueado. Contacte al administrador.");
        }

        if (Estado == EstadoUsuario.PendienteAprobacion)
        {
            throw new ReglaNegocioException("El usuario está pendiente de aprobación.");
        }

        if (Estado != EstadoUsuario.Activo)
        {
            throw new ReglaNegocioException("El usuario está inactivo.");
        }
    }

    public void RegistrarInicioSesion()
    {
        ValidarPuedeIngresar();
        IntentosFallidos = 0;
        SesionActiva = true;
        Tocar();
    }

    public void RegistrarIntentoFallido()
    {
        IntentosFallidos++;
        if (IntentosFallidos >= IntentosMaximos)
        {
            Estado = EstadoUsuario.Bloqueado;
            SesionActiva = false;
        }

        Tocar();
    }

    public void CerrarSesion()
    {
        SesionActiva = false;
        Tocar();
    }

    public void Aprobar(int administradorId)
    {
        if (Estado != EstadoUsuario.PendienteAprobacion)
        {
            throw new ReglaNegocioException("El usuario no está pendiente de aprobación.");
        }

        Estado = EstadoUsuario.Activo;
        AprobadoPorId = administradorId;
        FechaAprobacion = DateTime.UtcNow;
        IntentosFallidos = 0;
        Tocar();
    }

    public void Rechazar(int administradorId)
    {
        if (Estado != EstadoUsuario.PendienteAprobacion)
        {
            throw new ReglaNegocioException("El usuario no está pendiente de aprobación.");
        }

        Estado = EstadoUsuario.Inactivo;
        AprobadoPorId = administradorId;
        FechaAprobacion = DateTime.UtcNow;
        Tocar();
    }

    public void CambiarEstado(EstadoUsuario estado)
    {
        if (estado == EstadoUsuario.PendienteAprobacion)
        {
            throw new ReglaNegocioException("El estado pendiente solo se asigna al crear el usuario.");
        }

        Estado = estado;
        if (estado != EstadoUsuario.Activo)
        {
            SesionActiva = false;
        }

        if (estado == EstadoUsuario.Activo)
        {
            IntentosFallidos = 0;
        }

        Tocar();
    }

    public void ActualizarPassword(string hash)
    {
        PasswordHash = hash;
        IntentosFallidos = 0;
        if (Estado == EstadoUsuario.Bloqueado)
        {
            Estado = EstadoUsuario.Activo;
        }

        Tocar();
    }
}
