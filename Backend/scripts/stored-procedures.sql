/*
    NetDesk ISP
    Procedimientos y función usados por la API.
    Ejecutar sobre la base NetDeskIsp después de crear el esquema.
*/

CREATE OR ALTER FUNCTION dbo.fn_TotalTurnosRango
(
    @UsuarioId INT,
    @Rol NVARCHAR(30),
    @Inicio DATETIME2,
    @Fin DATETIME2
)
RETURNS INT
AS
BEGIN
    DECLARE @Total INT;

    IF @Rol = N'Administrador'
        SELECT @Total = COUNT(*)
        FROM dbo.Turnos
        WHERE Eliminado = 0
          AND Estado = 2
          AND FechaFinAtencion >= @Inicio
          AND FechaFinAtencion < @Fin;
    ELSE IF @Rol = N'Gestor'
        SELECT @Total = COUNT(*)
        FROM dbo.Turnos
        WHERE Eliminado = 0
          AND Estado <> 3
          AND GestorAsignadorId = @UsuarioId
          AND FechaAsignacion >= @Inicio
          AND FechaAsignacion < @Fin;
    ELSE
        SELECT @Total = COUNT(*)
        FROM dbo.Turnos
        WHERE Eliminado = 0
          AND Estado = 2
          AND CajeroId = @UsuarioId
          AND FechaFinAtencion >= @Inicio
          AND FechaFinAtencion < @Fin;

    RETURN ISNULL(@Total, 0);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GenerarCodigoTurno
    @Prefijo CHAR(2),
    @Codigo NVARCHAR(6) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Prefijo IS NULL OR LEN(@Prefijo) <> 2
        THROW 50001, 'El prefijo del turno debe tener 2 letras.', 1;

    DECLARE @Maximo INT;

    SELECT @Maximo = ISNULL(MAX(TRY_CAST(RIGHT(Descripcion, 4) AS INT)), 0)
    FROM dbo.Turnos WITH (UPDLOCK, HOLDLOCK)
    WHERE Descripcion LIKE @Prefijo + '[0-9][0-9][0-9][0-9]';

    SET @Codigo = UPPER(@Prefijo) + RIGHT('0000' + CAST(@Maximo + 1 AS VARCHAR(4)), 4);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_DashboardIndicadores
    @FechaInicio DATETIME2,
    @FechaFin DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SUM(CASE WHEN SesionActiva = 1 THEN 1 ELSE 0 END) AS SesionActiva,
        SUM(CASE WHEN SesionActiva = 0 AND Estado = 1 THEN 1 ELSE 0 END) AS SesionInactiva,
        SUM(CASE WHEN Estado = 3 THEN 1 ELSE 0 END) AS Bloqueados,
        SUM(CASE WHEN Estado = 0 THEN 1 ELSE 0 END) AS Pendientes,
        SUM(CASE WHEN Estado = 1 THEN 1 ELSE 0 END) AS Activos,
        SUM(CASE WHEN Estado = 2 THEN 1 ELSE 0 END) AS Inactivos
    FROM dbo.Usuarios
    WHERE Eliminado = 0;

    SELECT
        c.Id,
        c.Codigo,
        c.Nombre,
        (SELECT COUNT(*)
         FROM dbo.Turnos t
         WHERE t.CajaId = c.Id
           AND t.Eliminado = 0
           AND t.FechaAsignacion >= @FechaInicio
           AND t.FechaAsignacion < @FechaFin) AS TurnosAsignados,
        (SELECT COUNT(*)
         FROM dbo.Turnos t
         WHERE t.CajaId = c.Id
           AND t.Eliminado = 0
           AND t.Estado = 2
           AND t.FechaFinAtencion >= @FechaInicio
           AND t.FechaFinAtencion < @FechaFin) AS TurnosAtendidos
    FROM dbo.Cajas c
    WHERE c.Eliminado = 0
    ORDER BY c.Codigo;

    SELECT
        u.Id,
        CONCAT(u.Nombres, ' ', u.Apellidos) AS Nombre,
        u.NombreUsuario AS Usuario,
        (SELECT COUNT(*)
         FROM dbo.Turnos t
         WHERE t.CajeroId = u.Id
           AND t.Eliminado = 0
           AND t.Estado = 2
           AND t.FechaFinAtencion >= @FechaInicio
           AND t.FechaFinAtencion < @FechaFin) AS Total
    FROM dbo.Usuarios u
    INNER JOIN dbo.Roles r ON r.Id = u.RolId
    WHERE u.Eliminado = 0
      AND r.Nombre = N'Cajero'
    ORDER BY u.NombreUsuario;

    SELECT
        u.Id,
        CONCAT(u.Nombres, ' ', u.Apellidos) AS Nombre,
        u.NombreUsuario AS Usuario,
        (SELECT COUNT(*)
         FROM dbo.Turnos t
         WHERE t.GestorAsignadorId = u.Id
           AND t.Eliminado = 0
           AND t.FechaAsignacion >= @FechaInicio
           AND t.FechaAsignacion < @FechaFin) AS Total
    FROM dbo.Usuarios u
    INNER JOIN dbo.Roles r ON r.Id = u.RolId
    WHERE u.Eliminado = 0
      AND r.Nombre = N'Gestor'
    ORDER BY u.NombreUsuario;
END
GO
