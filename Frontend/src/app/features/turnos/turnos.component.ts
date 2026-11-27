import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Caja, Catalogo, Turno, Usuario } from '../../core/modelos';
import { NotificacionService } from '../../core/servicios';

@Component({
  selector: 'app-turnos',
  templateUrl: './turnos.component.html',
  standalone: false
})
export class TurnosComponent implements OnInit {
  cajas: Caja[] = [];
  cajeros: Usuario[] = [];
  tipos: Catalogo[] = [];
  turnos: Turno[] = [];
  private fb = inject(FormBuilder);
  asignacion = this.fb.nonNullable.group({
    cajaId: [0, Validators.min(1)],
    cajeroId: [0, Validators.min(1)]
  });
  turno = this.fb.nonNullable.group({
    cajaId: [0, Validators.min(1)],
    cajeroId: [0, Validators.min(1)],
    tipoAtencionId: [0, Validators.min(1)],
    cantidad: [1, [Validators.required, Validators.min(1), Validators.max(20)]]
  });

  constructor(private http: HttpClient, private avisos: NotificacionService) {}

  ngOnInit(): void {
    this.http.get<Usuario[]>('/api/usuarios/por-rol/Cajero').subscribe(cajeros => this.cajeros = cajeros);
    this.http.get<Catalogo[]>('/api/catalogos/tipos-atencion').subscribe(tipos => this.tipos = tipos);
    this.cargar();
  }

  cargar(): void {
    this.http.get<Caja[]>('/api/cajas').subscribe(cajas => this.cajas = cajas);
    this.http.get<Turno[]>('/api/turnos').subscribe(turnos => this.turnos = turnos);
  }

  cajerosDeCaja(cajaId: number): { id: number; nombre: string }[] {
    const caja = this.cajas.find(item => item.id === cajaId);
    return (caja?.asignaciones ?? []).map(item => ({ id: item.usuarioId, nombre: `${item.cajero} (${item.usuarioCajero})` }));
  }

  asignar(): void {
    if (this.asignacion.invalid) {
      this.asignacion.markAllAsTouched();
      return;
    }
    const { cajaId, cajeroId } = this.asignacion.getRawValue();
    this.http.post(`/api/cajas/${cajaId}/asignaciones`, { cajeroId }).subscribe(() => {
      this.avisos.ok('Cajero asignado a la caja.');
      this.cargar();
    });
  }

  quitar(cajaId: number, asignacionId: number): void {
    this.http.delete(`/api/cajas/${cajaId}/asignaciones/${asignacionId}`).subscribe(() => {
      this.avisos.ok('Asignación retirada.');
      this.cargar();
    });
  }

  crearTurnos(): void {
    if (this.turno.invalid) {
      this.turno.markAllAsTouched();
      return;
    }
    this.http.post('/api/turnos', this.turno.getRawValue()).subscribe(() => {
      this.avisos.ok('Turnos asignados.');
      this.cargar();
    });
  }

  cancelar(turno: Turno): void {
    this.http.delete(`/api/turnos/${turno.id}`).subscribe(() => {
      this.avisos.ok(`Turno ${turno.descripcion} cancelado.`);
      this.cargar();
    });
  }
}
