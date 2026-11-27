import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { Caja, Catalogo, Cliente, Contrato, Pago, Servicio, Turno } from '../../core/modelos';
import { AuthService, NotificacionService } from '../../core/servicios';
import { correoPattern, identificacionPattern, longitudEntre, nombrePattern, telefonoPattern } from '../../core/validadores';

@Component({
  selector: 'app-caja',
  templateUrl: './caja.component.html',
  standalone: false
})
export class CajaComponent implements OnInit {
  cajas: Caja[] = [];
  turnos: Turno[] = [];
  servicios: Servicio[] = [];
  formas: Catalogo[] = [];
  contratos: Contrato[] = [];
  pagos: Pago[] = [];
  cajaAbierta?: Caja;
  turno?: Turno;
  cliente?: Cliente;
  identificacion = '';
  mostrarRegistro = false;
  servicioId: number | null = null;
  formaPagoId: number | null = null;
  referencia = '';
  observacion = '';
  private fb = inject(FormBuilder);
  registro = this.fb.nonNullable.group({
    nombres: ['', [Validators.required, Validators.pattern(nombrePattern)]],
    apellidos: ['', [Validators.required, Validators.pattern(nombrePattern)]],
    identificacion: ['', [Validators.required, Validators.pattern(identificacionPattern)]],
    correo: ['', [Validators.required, Validators.pattern(correoPattern)]],
    telefono: ['', [Validators.required, Validators.pattern(telefonoPattern)]],
    direccion: ['', [Validators.required, longitudEntre(20, 100)]],
    referenciaDireccion: ['', [Validators.required, longitudEntre(20, 100)]]
  });

  constructor(
    private http: HttpClient,
    private auth: AuthService,
    private avisos: NotificacionService) {}

  ngOnInit(): void { this.cargar(); }

  get contratoVigente(): Contrato | undefined {
    return this.contratos.find(contrato => contrato.operable);
  }

  cargar(): void {
    forkJoin({
      cajas: this.http.get<Caja[]>('/api/cajas/mias'),
      turnos: this.http.get<Turno[]>('/api/turnos/mios'),
      servicios: this.http.get<Servicio[]>('/api/servicios', { params: { soloActivos: true } }),
      formas: this.http.get<Catalogo[]>('/api/catalogos/formas-pago')
    }).subscribe(({ cajas, turnos, servicios, formas }) => {
      this.cajas = cajas;
      this.turnos = turnos;
      this.servicios = servicios;
      this.formas = formas;
      this.cajaAbierta = cajas.find(caja => caja.usuarioActivoId === this.auth.usuario?.id);
      this.turno = turnos.find(turno => turno.estado === 'En atención');
      if (this.turno?.clienteId) {
        this.http.get<Cliente>(`/api/clientes/${this.turno.clienteId}`).subscribe(cliente => {
          this.cliente = cliente;
          this.cargarHistorial(cliente.id);
        });
      }
    });
  }

  abrir(caja: Caja): void {
    this.http.post<Caja>(`/api/cajas/${caja.id}/abrir`, {}).subscribe(abierta => {
      this.cajaAbierta = abierta;
      this.avisos.ok(`${caja.codigo} abierta. El otro cajero no puede operarla.`);
      this.cargar();
    });
  }

  cerrar(): void {
    if (!this.cajaAbierta) return;
    this.http.post(`/api/cajas/${this.cajaAbierta.id}/cerrar`, {}).subscribe(() => {
      this.avisos.ok('Caja cerrada.');
      this.cajaAbierta = undefined;
      this.cargar();
    });
  }

  iniciar(turno: Turno): void {
    this.http.post<Turno>(`/api/turnos/${turno.id}/iniciar`, {}).subscribe(activo => {
      this.turno = activo;
      this.cliente = undefined;
      this.contratos = [];
      this.avisos.ok(`Turno ${activo.descripcion} en atención.`);
      this.cargar();
    });
  }

  buscar(): void {
    const identificacion = this.identificacion.trim();
    if (!/^\d{10,13}$/.test(identificacion)) {
      this.avisos.error('La identificación debe tener entre 10 y 13 dígitos.');
      return;
    }
    const headers = new HttpHeaders({ 'X-Silenciar': '1' });
    this.http.get<Cliente>(`/api/clientes/por-identificacion/${identificacion}`, { headers }).subscribe({
      next: cliente => {
        this.cliente = cliente;
        this.mostrarRegistro = false;
        this.cargarHistorial(cliente.id);
      },
      error: () => {
        this.cliente = undefined;
        this.contratos = [];
        this.pagos = [];
        this.mostrarRegistro = true;
        this.registro.patchValue({ identificacion });
        this.avisos.ok('No hay cliente con esa identificación. Regístrelo para continuar.');
      }
    });
  }

  registrar(): void {
    if (this.registro.invalid) {
      this.registro.markAllAsTouched();
      return;
    }
    this.http.post<Cliente>('/api/clientes', this.registro.getRawValue()).subscribe(cliente => {
      this.cliente = cliente;
      this.mostrarRegistro = false;
      this.contratos = [];
      this.pagos = [];
      this.avisos.ok('Cliente registrado.');
    });
  }

  contratar(): void {
    if (!this.cliente || !this.turno || !this.servicioId || !this.formaPagoId) {
      this.avisos.error('Seleccione el servicio y la forma de pago.');
      return;
    }
    this.http.post('/api/contratos', {
      clienteId: this.cliente.id,
      servicioId: this.servicioId,
      formaPagoId: this.formaPagoId,
      turnoId: this.turno.id
    }).subscribe(() => this.finalizar('Contrato registrado y turno atendido.'));
  }

  pagar(): void {
    if (!this.contratoVigente || !this.turno) return;
    this.http.post('/api/pagos', {
      contratoId: this.contratoVigente.id,
      turnoId: this.turno.id,
      referencia: this.referencia || null
    }).subscribe(() => this.finalizar('Pago registrado y turno atendido.'));
  }

  cambiarServicio(): void {
    if (!this.contratoVigente || !this.turno || !this.servicioId) {
      this.avisos.error('Seleccione el nuevo servicio.');
      return;
    }
    this.http.post(`/api/contratos/${this.contratoVigente.id}/cambiar-servicio`, {
      servicioId: this.servicioId,
      turnoId: this.turno.id
    }).subscribe(() => this.finalizar('Servicio renovado. El contrato anterior quedó sustituido.'));
  }

  cambiarPago(): void {
    if (!this.contratoVigente || !this.turno || !this.formaPagoId) {
      this.avisos.error('Seleccione la nueva forma de pago.');
      return;
    }
    this.http.post(`/api/contratos/${this.contratoVigente.id}/cambiar-forma-pago`, {
      formaPagoId: this.formaPagoId,
      turnoId: this.turno.id
    }).subscribe(() => this.finalizar('Forma de pago actualizada y turno atendido.'));
  }

  cancelar(): void {
    if (!this.contratoVigente || !this.turno) return;
    this.http.post(`/api/contratos/${this.contratoVigente.id}/cancelar`, {
      turnoId: this.turno.id,
      observacion: this.observacion
    }).subscribe(() => this.finalizar('Contrato cancelado y turno atendido.'));
  }

  private cargarHistorial(clienteId: number): void {
    this.http.get<Contrato[]>(`/api/contratos/cliente/${clienteId}`).subscribe(contratos => this.contratos = contratos);
    this.http.get<Pago[]>(`/api/pagos/cliente/${clienteId}`).subscribe(pagos => this.pagos = pagos);
  }

  private finalizar(mensaje: string): void {
    if (!this.turno) return;
    this.http.post(`/api/turnos/${this.turno.id}/finalizar`, {}).subscribe(() => {
      this.avisos.ok(mensaje);
      this.turno = undefined;
      this.cliente = undefined;
      this.identificacion = '';
      this.cargar();
    });
  }
}
