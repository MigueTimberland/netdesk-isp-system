using NetDesk.Application.Abstracciones;
using NetDesk.Application.Comun;
using NetDesk.Application.Dtos;
using NetDesk.Domain.Entities;
using NetDesk.Domain.Enums;

namespace NetDesk.Application.Servicios;

public sealed class Mapeador(ICifradoService cifrado)
{
    public UsuarioSesionDto Sesion(Usuario usuario) => new(
        usuario.Id,
        usuario.NombreUsuario,
        usuario.Nombres,
        usuario.Apellidos,
        usuario.NombreCompleto,
        usuario.Rol.Nombre,
        cifrado.Descifrar(usuario.CorreoCifrado),
        usuario.Telefono,
        cifrado.Descifrar(usuario.IdentificacionCifrada),
        NombresEstado.Usuario(usuario.Estado),
        usuario.SesionActiva);

    public UsuarioDto Usuario(Usuario usuario) => new(
        usuario.Id,
        usuario.NombreUsuario,
        usuario.Nombres,
        usuario.Apellidos,
        cifrado.Descifrar(usuario.IdentificacionCifrada),
        cifrado.Descifrar(usuario.CorreoCifrado),
        usuario.Telefono,
        usuario.RolId,
        usuario.Rol.Nombre,
        NombresEstado.Usuario(usuario.Estado),
        usuario.SesionActiva,
        usuario.IntentosFallidos,
        usuario.FechaCreacion);

    public ClienteDto Cliente(Cliente cliente) => new(
        cliente.Id,
        cliente.Nombres,
        cliente.Apellidos,
        cliente.NombreCompleto,
        cifrado.Descifrar(cliente.IdentificacionCifrada),
        cifrado.Descifrar(cliente.CorreoCifrado),
        cliente.Telefono,
        cliente.Direccion,
        cliente.ReferenciaDireccion,
        NombresEstado.Cliente(cliente.Estado),
        cliente.FechaCreacion);

    public CajaDto Caja(Caja caja) => new(
        caja.Id,
        caja.Codigo,
        caja.Nombre,
        caja.Descripcion,
        caja.Estado == EstadoCaja.Activa ? "Activa" : "Inactiva",
        caja.UsuarioActivoId,
        caja.UsuarioActivo is null ? null : caja.UsuarioActivo.NombreCompleto,
        caja.FechaAperturaSesion,
        caja.Asignaciones
            .Where(a => a.Activa && !a.Eliminado)
            .Select(a => new AsignacionCajaDto(a.Id, a.UsuarioId, a.Usuario.NombreCompleto, a.Usuario.NombreUsuario, a.FechaCreacion))
            .ToList());

    public TurnoDto Turno(Turno turno) => new(
        turno.Id,
        turno.Descripcion,
        turno.TipoAtencionId,
        turno.TipoAtencion.Codigo,
        turno.TipoAtencion.Nombre,
        turno.CajaId,
        turno.Caja.Nombre,
        turno.CajeroId,
        turno.Cajero.NombreCompleto,
        turno.GestorAsignadorId,
        turno.GestorAsignador.NombreCompleto,
        turno.ClienteId,
        turno.Cliente?.NombreCompleto,
        NombresEstado.Turno(turno.Estado),
        turno.ProcesoRegistrado,
        turno.FechaAsignacion,
        turno.FechaInicioAtencion,
        turno.FechaFinAtencion);

    public ServicioDto Servicio(Servicio servicio) => new(
        servicio.Id,
        servicio.Nombre,
        servicio.Descripcion,
        servicio.Equipos,
        servicio.VelocidadMbps,
        servicio.Precio,
        servicio.Activo);

    public ContratoDto Contrato(Contrato contrato)
    {
        var estado = NombresEstado.Contrato(contrato.Estado);
        return new ContratoDto(
            contrato.Id,
            contrato.Numero,
            contrato.ClienteId,
            contrato.Cliente.NombreCompleto,
            contrato.ServicioId,
            contrato.Servicio.Nombre,
            contrato.Servicio.VelocidadMbps,
            contrato.Servicio.Precio,
            contrato.FormaPagoId,
            contrato.FormaPago.Nombre,
            estado.Codigo,
            estado.Nombre,
            contrato.ContratoOrigenId,
            contrato.FechaInicio,
            contrato.FechaFin,
            contrato.EsOperable);
    }

    public PagoDto Pago(Pago pago) => new(
        pago.Id,
        pago.ContratoId,
        pago.Contrato.Numero,
        pago.ClienteId,
        pago.Monto,
        pago.FechaPago,
        pago.FormaPago.Nombre,
        pago.Referencia,
        pago.Cajero.NombreCompleto);
}
