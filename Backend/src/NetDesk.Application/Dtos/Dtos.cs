namespace NetDesk.Application.Dtos;

public record LoginRequest(string NombreUsuario, string Password);
public record RecuperarPasswordRequest(string Correo);
public record RestablecerPasswordRequest(string Correo, string Codigo, string NuevaPassword);

public record UsuarioSesionDto(
    int Id,
    string NombreUsuario,
    string Nombres,
    string Apellidos,
    string NombreCompleto,
    string Rol,
    string Correo,
    string Telefono,
    string Identificacion,
    string Estado,
    bool SesionActiva);

public record LoginResponse(string Token, DateTime ExpiraEn, string TipoToken, UsuarioSesionDto Usuario);
public record RecuperarPasswordResponse(string Mensaje, string? CodigoDemostracion);

public record UsuarioDto(
    int Id,
    string NombreUsuario,
    string Nombres,
    string Apellidos,
    string Identificacion,
    string Correo,
    string Telefono,
    int RolId,
    string Rol,
    string Estado,
    bool SesionActiva,
    int IntentosFallidos,
    DateTime FechaCreacion);

public record CrearUsuarioRequest(
    string NombreUsuario,
    string Password,
    string Nombres,
    string Apellidos,
    string Identificacion,
    string Correo,
    string Telefono,
    int RolId);

public record ActualizarUsuarioRequest(
    string NombreUsuario,
    string? Password,
    string Nombres,
    string Apellidos,
    string Identificacion,
    string Correo,
    string Telefono,
    int RolId);

public record CambiarEstadoRequest(string Estado);

public record CargaMasivaResultadoDto(int Creados, int Fallidos, IReadOnlyList<string> Errores);

public record ClienteDto(
    int Id,
    string Nombres,
    string Apellidos,
    string NombreCompleto,
    string Identificacion,
    string Correo,
    string Telefono,
    string Direccion,
    string ReferenciaDireccion,
    string Estado,
    DateTime FechaCreacion);

public record GuardarClienteRequest(
    string Nombres,
    string Apellidos,
    string Identificacion,
    string Correo,
    string Telefono,
    string Direccion,
    string ReferenciaDireccion);

public record CajaDto(
    int Id,
    string Codigo,
    string Nombre,
    string Descripcion,
    string Estado,
    int? UsuarioActivoId,
    string? UsuarioActivo,
    DateTime? FechaAperturaSesion,
    IReadOnlyList<AsignacionCajaDto> Asignaciones);

public record AsignacionCajaDto(int Id, int UsuarioId, string Cajero, string UsuarioCajero, DateTime FechaAsignacion);
public record CrearCajaRequest(string Codigo, string Nombre, string Descripcion);
public record AsignarCajeroRequest(int CajeroId);

public record TurnoDto(
    int Id,
    string Descripcion,
    int TipoAtencionId,
    string TipoCodigo,
    string TipoNombre,
    int CajaId,
    string Caja,
    int CajeroId,
    string Cajero,
    int GestorId,
    string Gestor,
    int? ClienteId,
    string? Cliente,
    string Estado,
    bool ProcesoRegistrado,
    DateTime FechaAsignacion,
    DateTime? FechaInicioAtencion,
    DateTime? FechaFinAtencion);

public record CrearTurnosRequest(int CajaId, int CajeroId, int TipoAtencionId, int Cantidad);
public record IniciarTurnoRequest(int? ClienteId);

public record ServicioDto(int Id, string Nombre, string Descripcion, string Equipos, int VelocidadMbps, decimal Precio, bool Activo);
public record GuardarServicioRequest(string Nombre, string Descripcion, string Equipos, int VelocidadMbps, decimal Precio, bool Activo);
public record CatalogoDto(int Id, string Nombre, string? Codigo);

public record ContratoDto(
    int Id,
    string Numero,
    int ClienteId,
    string Cliente,
    int ServicioId,
    string Servicio,
    int VelocidadMbps,
    decimal Precio,
    int FormaPagoId,
    string FormaPago,
    string EstadoCodigo,
    string EstadoNombre,
    int? ContratoOrigenId,
    DateTime FechaInicio,
    DateTime FechaFin,
    bool Operable);

public record ContratarRequest(int ClienteId, int ServicioId, int FormaPagoId, int TurnoId);
public record CambiarServicioRequest(int ServicioId, int TurnoId);
public record CambiarFormaPagoRequest(int FormaPagoId, int TurnoId);
public record CancelarContratoRequest(int TurnoId, string? Observacion);

public record PagoDto(
    int Id,
    int ContratoId,
    string NumeroContrato,
    int ClienteId,
    decimal Monto,
    DateTime FechaPago,
    string FormaPago,
    string? Referencia,
    string Cajero);

public record RegistrarPagoRequest(int ContratoId, int TurnoId, string? Referencia);

public record MenuDto(int Id, string Titulo, string Ruta, string Icono, int Orden);

public record BienvenidaDto(
    UsuarioSesionDto Usuario,
    int TotalTurnos,
    string DescripcionTotal,
    IReadOnlyList<UsuarioDto> Pendientes);

public record IndicadoresUsuariosDto(int SesionActiva, int SesionInactiva, int Bloqueados, int Pendientes, int Activos, int Inactivos);
public record IndicadorCajaDto(int Id, string Codigo, string Nombre, int TurnosAsignados, int TurnosAtendidos);
public record IndicadorPersonaDto(int Id, string Nombre, string Usuario, int Total);

public record DashboardDto(
    DateOnly FechaInicio,
    DateOnly FechaFin,
    IndicadoresUsuariosDto Usuarios,
    IReadOnlyList<IndicadorCajaDto> Cajas,
    IReadOnlyList<IndicadorPersonaDto> Cajeros,
    IReadOnlyList<IndicadorPersonaDto> Gestores);
