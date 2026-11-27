export interface UsuarioSesion {
  id: number;
  nombreUsuario: string;
  nombres: string;
  apellidos: string;
  nombreCompleto: string;
  rol: string;
  correo: string;
  telefono: string;
  identificacion: string;
  estado: string;
  sesionActiva: boolean;
}

export interface LoginResponse {
  token: string;
  expiraEn: string;
  tipoToken: string;
  usuario: UsuarioSesion;
}

export interface Usuario extends UsuarioSesion {
  rolId: number;
  intentosFallidos: number;
  fechaCreacion: string;
}

export interface Cliente {
  id: number;
  nombres: string;
  apellidos: string;
  nombreCompleto: string;
  identificacion: string;
  correo: string;
  telefono: string;
  direccion: string;
  referenciaDireccion: string;
  estado: string;
  fechaCreacion: string;
}

export interface AsignacionCaja {
  id: number;
  usuarioId: number;
  cajero: string;
  usuarioCajero: string;
  fechaAsignacion: string;
}

export interface Caja {
  id: number;
  codigo: string;
  nombre: string;
  descripcion: string;
  estado: string;
  usuarioActivoId: number | null;
  usuarioActivo: string | null;
  fechaAperturaSesion: string | null;
  asignaciones: AsignacionCaja[];
}

export interface Turno {
  id: number;
  descripcion: string;
  tipoAtencionId: number;
  tipoCodigo: string;
  tipoNombre: string;
  cajaId: number;
  caja: string;
  cajeroId: number;
  cajero: string;
  gestorId: number;
  gestor: string;
  clienteId: number | null;
  cliente: string | null;
  estado: string;
  procesoRegistrado: boolean;
  fechaAsignacion: string;
  fechaInicioAtencion: string | null;
  fechaFinAtencion: string | null;
}

export interface Servicio {
  id: number;
  nombre: string;
  descripcion: string;
  equipos: string;
  velocidadMbps: number;
  precio: number;
  activo: boolean;
}

export interface Catalogo {
  id: number;
  nombre: string;
  codigo: string | null;
}

export interface Contrato {
  id: number;
  numero: string;
  clienteId: number;
  cliente: string;
  servicioId: number;
  servicio: string;
  velocidadMbps: number;
  precio: number;
  formaPagoId: number;
  formaPago: string;
  estadoCodigo: string;
  estadoNombre: string;
  contratoOrigenId: number | null;
  fechaInicio: string;
  fechaFin: string;
  operable: boolean;
}

export interface Pago {
  id: number;
  contratoId: number;
  numeroContrato: string;
  clienteId: number;
  monto: number;
  fechaPago: string;
  formaPago: string;
  referencia: string | null;
  cajero: string;
}

export interface Menu {
  id: number;
  titulo: string;
  ruta: string;
  icono: string;
  orden: number;
}

export interface Bienvenida {
  usuario: UsuarioSesion;
  totalTurnos: number;
  descripcionTotal: string;
  pendientes: Usuario[];
}

export interface Dashboard {
  fechaInicio: string;
  fechaFin: string;
  usuarios: {
    sesionActiva: number;
    sesionInactiva: number;
    bloqueados: number;
    pendientes: number;
    activos: number;
    inactivos: number;
  };
  cajas: { id: number; codigo: string; nombre: string; turnosAsignados: number; turnosAtendidos: number }[];
  cajeros: { id: number; nombre: string; usuario: string; total: number }[];
  gestores: { id: number; nombre: string; usuario: string; total: number }[];
}

export interface CargaMasiva {
  creados: number;
  fallidos: number;
  errores: string[];
}

export interface Recuperacion {
  mensaje: string;
  codigoDemostracion: string | null;
}
