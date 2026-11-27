import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Servicio } from '../../core/modelos';
import { NotificacionService } from '../../core/servicios';

@Component({
  selector: 'app-servicios',
  templateUrl: './servicios.component.html',
  standalone: false
})
export class ServiciosComponent implements OnInit {
  servicios: Servicio[] = [];
  editando: Servicio | null = null;
  private fb = inject(FormBuilder);
  form = this.fb.nonNullable.group({
    nombre: ['', Validators.required],
    descripcion: ['', Validators.required],
    equipos: ['', Validators.required],
    velocidadMbps: [50, [Validators.required, Validators.min(1)]],
    precio: [20, [Validators.required, Validators.min(0.01)]],
    activo: [true]
  });

  constructor(private http: HttpClient, private avisos: NotificacionService) {}

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.http.get<Servicio[]>('/api/servicios').subscribe(servicios => this.servicios = servicios);
  }

  editar(servicio: Servicio): void {
    this.editando = servicio;
    this.form.patchValue(servicio);
  }

  nuevo(): void {
    this.editando = null;
    this.form.reset({ nombre: '', descripcion: '', equipos: '', velocidadMbps: 50, precio: 20, activo: true });
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const valor = this.form.getRawValue();
    const peticion = this.editando
      ? this.http.put(`/api/servicios/${this.editando.id}`, valor)
      : this.http.post('/api/servicios', valor);
    peticion.subscribe(() => {
      this.avisos.ok('Servicio guardado.');
      this.nuevo();
      this.cargar();
    });
  }

  eliminar(servicio: Servicio): void {
    if (!confirm(`¿Eliminar ${servicio.nombre}?`)) return;
    this.http.delete(`/api/servicios/${servicio.id}`).subscribe(() => {
      this.avisos.ok('Servicio eliminado.');
      this.cargar();
    });
  }
}
