using NetDesk.Domain.Common;
using NetDesk.Domain.Enums;

namespace NetDesk.Domain.Entities;

public class Turno : EntidadAuditable
{
    public string Descripcion { get; set; } = string.Empty;
    public int TipoAtencionId { get; set; }
    public TipoAtencion TipoAtencion { get; set; } = null!;
    public int CajaId { get; set; }
    public Caja Caja { get; set; } = null!;
    public int CajeroId { get; set; }
    public Usuario Cajero { get; set; } = null!;
    public int GestorAsignadorId { get; set; }
    public Usuario GestorAsignador { get; set; } = null!;
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public EstadoTurno Estado { get; set; } = EstadoTurno.Asignado;
    public bool ProcesoRegistrado { get; set; }
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaInicioAtencion { get; set; }
    public DateTime? FechaFinAtencion { get; set; }

    public void Iniciar(int cajeroId, int? clienteId)
    {
        if (Estado != EstadoTurno.Asignado || Eliminado)
        {
            throw new ReglaNegocioException("Solo se puede atender un turno asignado.");
        }

        if (CajeroId != cajeroId)
        {
            throw new ReglaNegocioException("Este turno está asignado a otro cajero.");
        }

        Estado = EstadoTurno.EnAtencion;
        FechaInicioAtencion = DateTime.UtcNow;
        if (clienteId.HasValue)
        {
            ClienteId = clienteId;
        }

        Tocar();
    }

    public void VincularCliente(int clienteId)
    {
        if (Estado != EstadoTurno.EnAtencion)
        {
            throw new ReglaNegocioException("El turno no está en atención.");
        }

        if (ClienteId.HasValue && ClienteId != clienteId)
        {
            throw new ReglaNegocioException("El turno ya está vinculado a otro cliente.");
        }

        ClienteId = clienteId;
        Tocar();
    }

    public void MarcarProceso()
    {
        if (Estado != EstadoTurno.EnAtencion)
        {
            throw new ReglaNegocioException("El turno no está en atención.");
        }

        ProcesoRegistrado = true;
        Tocar();
    }

    public void Finalizar()
    {
        if (Estado != EstadoTurno.EnAtencion)
        {
            throw new ReglaNegocioException("El turno no está en atención.");
        }

        if (!ProcesoRegistrado)
        {
            throw new ReglaNegocioException("Registre el proceso del turno antes de finalizarlo.");
        }

        Estado = EstadoTurno.Atendido;
        FechaFinAtencion = DateTime.UtcNow;
        Tocar();
    }

    public void Cancelar()
    {
        if (Estado is EstadoTurno.Atendido or EstadoTurno.Cancelado)
        {
            throw new ReglaNegocioException("El turno ya no se puede cancelar.");
        }

        if (Estado == EstadoTurno.EnAtencion)
        {
            throw new ReglaNegocioException("No se puede cancelar un turno que está en atención.");
        }

        Estado = EstadoTurno.Cancelado;
        Tocar();
    }
}
