import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Dashboard } from '../../core/modelos';
import { NotificacionService } from '../../core/servicios';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  standalone: false
})
export class DashboardComponent implements OnInit {
  datos?: Dashboard;
  hoy = fechaLocal();
  private fb = inject(FormBuilder);
  filtro = this.fb.nonNullable.group({
    desde: [this.hoy, Validators.required],
    hasta: [this.hoy, Validators.required]
  });
  caja = this.fb.nonNullable.group({
    codigo: ['', Validators.required],
    nombre: ['', Validators.required],
    descripcion: ['']
  });

  constructor(private http: HttpClient, private avisos: NotificacionService) {}

  ngOnInit(): void {
    this.consultar();
  }

  consultar(): void {
    if (this.filtro.invalid) {
      this.filtro.markAllAsTouched();
      return;
    }
    const { desde, hasta } = this.filtro.getRawValue();
    this.http.get<Dashboard>('/api/dashboard/indicadores', { params: { desde, hasta } })
      .subscribe(datos => this.datos = datos);
  }

  crearCaja(): void {
    if (this.caja.invalid) {
      this.caja.markAllAsTouched();
      return;
    }
    this.http.post('/api/cajas', this.caja.getRawValue()).subscribe(() => {
      this.avisos.ok('Caja creada.');
      this.caja.reset();
      this.consultar();
    });
  }
}

function fechaLocal(fecha = new Date()): string {
  const mes = `${fecha.getMonth() + 1}`.padStart(2, '0');
  const dia = `${fecha.getDate()}`.padStart(2, '0');
  return `${fecha.getFullYear()}-${mes}-${dia}`;
}
