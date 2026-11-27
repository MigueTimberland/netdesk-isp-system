using FluentValidation;
using Microsoft.Extensions.Logging;
using NetDesk.Application.Abstracciones;
using NetDesk.Application.Comun;
using NetDesk.Application.Dtos;
using NetDesk.Domain.Common;
using NetDesk.Domain.Entities;
using NetDesk.Domain.Enums;

namespace NetDesk.Application.Servicios;

public sealed class UsuarioService(
    IUsuarioRepositorio usuarios,
    ICatalogoRepositorio catalogos,
    ICajaRepositorio cajas,
    ICifradoService cifrado,
    IPasswordService passwords,
    IUnidadDeTrabajo unidad,
    IUsuarioActual actual,
    ILectorArchivoMasivo lector,
    Mapeador mapeador,
    IValidator<CrearUsuarioRequest> crearValidator,
    IValidator<ActualizarUsuarioRequest> actualizarValidator,
    IValidator<CambiarEstadoRequest> estadoValidator,
    ILogger<UsuarioService> logger)
{
    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken ct)
    {
        var lista = await usuarios.ListarAsync(ct);
        return lista.Where(u => u.Rol.Nombre != "Administrador").Select(mapeador.Usuario).ToList();
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarPendientesAsync(CancellationToken ct)
    {
        var lista = await usuarios.ListarPendientesAsync(ct);
        return lista.Select(mapeador.Usuario).ToList();
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarActivosPorRolAsync(string rol, CancellationToken ct)
    {
        var lista = await usuarios.ListarPorRolAsync(rol, ct);
        return lista.Where(u => u.Estado == EstadoUsuario.Activo).Select(mapeador.Usuario).ToList();
    }

    public async Task<UsuarioDto> ObtenerAsync(int id, CancellationToken ct)
    {
        var usuario = await ObtenerOperableAsync(id, ct);
        return mapeador.Usuario(usuario);
    }

    public async Task<UsuarioDto> CrearAsync(CrearUsuarioRequest request, CancellationToken ct)
    {
        var dto = await CrearInternoAsync(request, ct);
        unidad.LimpiarSeguimiento();
        return dto;
    }

    public async Task<UsuarioDto> ActualizarAsync(int id, ActualizarUsuarioRequest request, CancellationToken ct)
    {
        await actualizarValidator.ValidarAsync(request, ct);
        var usuario = await ObtenerOperableAsync(id, ct);
        var rol = await RolOperativoAsync(request.RolId, ct);
        await AsegurarUnicosAsync(request.NombreUsuario, request.Identificacion, request.Correo, usuario.Id, ct);

        usuario.NombreUsuario = Normalizador.Usuario(request.NombreUsuario);
        usuario.Nombres = Normalizador.Nombre(request.Nombres);
        usuario.Apellidos = Normalizador.Nombre(request.Apellidos);
        usuario.Telefono = Normalizador.Telefono(request.Telefono);
        var protegido = Proteger(request.Identificacion, request.Correo);
        usuario.IdentificacionCifrada = protegido.IdCifrada;
        usuario.IdentificacionIndice = protegido.IdIndice;
        usuario.CorreoCifrado = protegido.CorreoCifrado;
        usuario.CorreoIndice = protegido.CorreoIndice;
        usuario.RolId = rol.Id;
        usuario.Rol = rol;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            usuario.ActualizarPassword(passwords.Hash(request.Password));
        }
        else
        {
            usuario.Tocar();
        }

        await unidad.GuardarAsync(ct);
        logger.LogInformation("Actualización de usuario {NombreUsuario} por {Actor}.", usuario.NombreUsuario, actual.NombreUsuario);
        return mapeador.Usuario(usuario);
    }

    public async Task<UsuarioDto> CambiarEstadoAsync(int id, CambiarEstadoRequest request, CancellationToken ct)
    {
        await estadoValidator.ValidarAsync(request, ct);
        var usuario = await ObtenerOperableAsync(id, ct);
        var estado = NombresEstado.ParseUsuario(request.Estado);
        if (usuario.Id == actual.Id && estado != EstadoUsuario.Activo)
        {
            throw new ReglaNegocioException("No puede desactivar su propio usuario.");
        }

        if (usuario.Estado == EstadoUsuario.PendienteAprobacion && estado == EstadoUsuario.Activo)
        {
            usuario.Aprobar(actual.Id);
        }
        else
        {
            usuario.CambiarEstado(estado);
        }

        await unidad.GuardarAsync(ct);
        logger.LogInformation("Actualización de estado de {NombreUsuario} a {Estado} por {Actor}.", usuario.NombreUsuario, usuario.Estado, actual.NombreUsuario);
        return mapeador.Usuario(usuario);
    }

    public async Task<UsuarioDto> AprobarAsync(int id, CancellationToken ct)
    {
        var usuario = await ObtenerOperableAsync(id, ct);
        usuario.Aprobar(actual.Id);
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Actualización: usuario {NombreUsuario} aprobado por {Actor}.", usuario.NombreUsuario, actual.NombreUsuario);
        return mapeador.Usuario(usuario);
    }

    public async Task<UsuarioDto> RechazarAsync(int id, CancellationToken ct)
    {
        var usuario = await ObtenerOperableAsync(id, ct);
        usuario.Rechazar(actual.Id);
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Actualización: usuario {NombreUsuario} rechazado por {Actor}.", usuario.NombreUsuario, actual.NombreUsuario);
        return mapeador.Usuario(usuario);
    }

    public async Task EliminarAsync(int id, CancellationToken ct)
    {
        var usuario = await ObtenerOperableAsync(id, ct);
        if (usuario.Id == actual.Id)
        {
            throw new ReglaNegocioException("No puede eliminar su propio usuario.");
        }

        var caja = await cajas.ObtenerAbiertaPorUsuarioAsync(usuario.Id, ct);
        if (caja is not null)
        {
            throw new ReglaNegocioException("El usuario tiene una caja abierta. Ciérrela antes de eliminarlo.");
        }

        usuario.MarcarEliminado();
        usuario.SesionActiva = false;
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Eliminación lógica de usuario {NombreUsuario} por {Actor}.", usuario.NombreUsuario, actual.NombreUsuario);
    }

    public async Task<CargaMasivaResultadoDto> CargarMasivoAsync(Stream archivo, string nombreArchivo, CancellationToken ct)
    {
        var filas = lector.LeerUsuarios(archivo, nombreArchivo);
        if (filas.Count == 0)
        {
            throw new ReglaNegocioException("El archivo no contiene filas para procesar.");
        }

        var errores = new List<string>();
        var creados = 0;
        foreach (var fila in filas)
        {
            try
            {
                var rol = await catalogos.ObtenerRolPorNombreAsync(fila.Rol.Trim(), ct)
                    ?? throw new ReglaNegocioException("El rol debe ser Gestor o Cajero.");
                if (rol.Nombre is not ("Gestor" or "Cajero"))
                {
                    throw new ReglaNegocioException("El rol debe ser Gestor o Cajero.");
                }

                await CrearInternoAsync(new CrearUsuarioRequest(
                    fila.NombreUsuario,
                    fila.Password,
                    fila.Nombres,
                    fila.Apellidos,
                    fila.Identificacion,
                    fila.Correo,
                    fila.Telefono,
                    rol.Id), ct);
                creados++;
            }
            catch (Exception ex) when (ex is ValidacionException or ReglaNegocioException)
            {
                var detalle = ex is ValidacionException validacion
                    ? string.Join(" ", validacion.Errores.SelectMany(e => e.Value))
                    : ex.Message;
                errores.Add($"Fila {fila.Fila}: {detalle}");
            }
            finally
            {
                unidad.LimpiarSeguimiento();
            }
        }

        logger.LogInformation("Carga masiva de usuarios. Creados {Creados}. Fallidos {Fallidos}. Actor {Actor}.", creados, errores.Count, actual.NombreUsuario);
        return new CargaMasivaResultadoDto(creados, errores.Count, errores);
    }

    public byte[] Plantilla() => lector.PlantillaUsuarios();

    private async Task<UsuarioDto> CrearInternoAsync(CrearUsuarioRequest request, CancellationToken ct)
    {
        await crearValidator.ValidarAsync(request, ct);
        var actor = await usuarios.ObtenerPorIdAsync(actual.Id, ct)
            ?? throw new ReglaNegocioException("La sesión no es válida.");
        if (actor.Rol.Nombre is not ("Administrador" or "Gestor"))
        {
            throw new ReglaNegocioException("No tiene permiso para crear usuarios.");
        }

        var rol = await RolOperativoAsync(request.RolId, ct);
        await AsegurarUnicosAsync(request.NombreUsuario, request.Identificacion, request.Correo, null, ct);
        var protegido = Proteger(request.Identificacion, request.Correo);
        var esAdmin = actor.Rol.Nombre == "Administrador";

        var usuario = new Usuario
        {
            NombreUsuario = Normalizador.Usuario(request.NombreUsuario),
            PasswordHash = passwords.Hash(request.Password),
            Nombres = Normalizador.Nombre(request.Nombres),
            Apellidos = Normalizador.Nombre(request.Apellidos),
            IdentificacionCifrada = protegido.IdCifrada,
            IdentificacionIndice = protegido.IdIndice,
            CorreoCifrado = protegido.CorreoCifrado,
            CorreoIndice = protegido.CorreoIndice,
            Telefono = Normalizador.Telefono(request.Telefono),
            RolId = rol.Id,
            Rol = rol,
            Estado = esAdmin ? EstadoUsuario.Activo : EstadoUsuario.PendienteAprobacion,
            CreadoPorId = actor.Id,
            AprobadoPorId = esAdmin ? actor.Id : null,
            FechaAprobacion = esAdmin ? DateTime.UtcNow : null
        };

        await usuarios.AgregarAsync(usuario, ct);
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Creación de usuario {NombreUsuario} con estado {Estado} por {Actor}.", usuario.NombreUsuario, usuario.Estado, actor.NombreUsuario);
        return mapeador.Usuario(usuario);
    }

    private async Task<Usuario> ObtenerOperableAsync(int id, CancellationToken ct)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Usuario no encontrado.");
        if (usuario.Rol.Nombre == "Administrador")
        {
            throw new ReglaNegocioException("El usuario administrador de plataforma no se modifica por esta pantalla.");
        }

        return usuario;
    }

    private async Task<Rol> RolOperativoAsync(int rolId, CancellationToken ct)
    {
        var rol = await catalogos.ObtenerRolAsync(rolId, ct)
            ?? throw new ReglaNegocioException("El rol no existe.");
        if (rol.Nombre is not ("Gestor" or "Cajero"))
        {
            throw new ReglaNegocioException("Solo se pueden gestionar usuarios gestores y cajeros.");
        }

        return rol;
    }

    private async Task AsegurarUnicosAsync(string nombre, string identificacion, string correo, int? exceptoId, CancellationToken ct)
    {
        if (await usuarios.ExisteNombreAsync(Normalizador.Usuario(nombre), exceptoId, ct))
        {
            throw new ReglaNegocioException("El nombre de usuario ya está registrado.");
        }

        var protegido = Proteger(identificacion, correo);
        if (await usuarios.ExisteIdentificacionAsync(protegido.IdIndice, exceptoId, ct))
        {
            throw new ReglaNegocioException("La identificación ya está registrada.");
        }

        if (await usuarios.ExisteCorreoAsync(protegido.CorreoIndice, exceptoId, ct))
        {
            throw new ReglaNegocioException("El correo ya está registrado.");
        }
    }

    private (string IdCifrada, string IdIndice, string CorreoCifrado, string CorreoIndice) Proteger(string identificacion, string correo)
    {
        var id = Normalizador.Identificacion(identificacion);
        var mail = Normalizador.Correo(correo);
        return (cifrado.Cifrar(id), cifrado.Indice(id), cifrado.Cifrar(mail), cifrado.Indice(mail));
    }
}
