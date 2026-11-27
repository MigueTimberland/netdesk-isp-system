import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { CargaMasiva, Cliente } from '../../core/modelos';
import { NotificacionService } from '../../core/servicios';
import { correoPattern, identificacionPattern, longitudEntre, nombrePattern, telefonoPattern } from '../../core/validadores';
import { descargar } from '../usuarios/usuarios.component';

@Component({
  selector: 'app-clientes',
  templateUrl: './clientes.component.html',
  standalone: false
})
export class ClientesComponent implements OnInit {
  clientes: Cliente[] = [];
  editando: Cliente | null = null;
  texto = '';
  archivo: File | null = null;
  resultado: CargaMasiva | null = null;
  private fb = inject(FormBuilder);
  form = this.fb.nonNullable.group({
    nombres: ['', [Validators.required, Validators.pattern(nombrePattern)]],
    apellidos: ['', [Validators.required, Validators.pattern(nombrePattern)]],
    identificacion: ['', [Validators.required, Validators.pattern(identificacionPattern)]],
    correo: ['', [Validators.required, Validators.pattern(correoPattern)]],
    telefono: ['', [Validators.required, Validators.pattern(telefonoPattern)]],
    direccion: ['', [Validators.required, longitudEntre(20, 100)]],
    referenciaDireccion: ['', [Validators.required, longitudEntre(20, 100)]]
  });

  constructor(private http: HttpClient, private avisos: NotificacionService) {}

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.http.get<Cliente[]>('/api/clientes', { params: this.texto ? { texto: this.texto } : {} })
      .subscribe(clientes => this.clientes = clientes);
  }

  invalido(campo: string): boolean {
    const control = this.form.get(campo);
    return !!control && control.touched && control.invalid;
  }

  editar(cliente: Cliente): void {
    this.editando = cliente;
    this.form.patchValue(cliente);
  }

  nuevo(): void {
    this.editando = null;
    this.form.reset();
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const valor = this.form.getRawValue();
    const peticion = this.editando
      ? this.http.put(`/api/clientes/${this.editando.id}`, valor)
      : this.http.post('/api/clientes', valor);
    peticion.subscribe(() => {
      this.avisos.ok(this.editando ? 'Cliente actualizado.' : 'Cliente registrado.');
      this.nuevo();
      this.cargar();
    });
  }

  estado(cliente: Cliente, estado: string): void {
    this.http.patch(`/api/clientes/${cliente.id}/estado`, { estado }).subscribe(() => {
      this.avisos.ok('Estado actualizado.');
      this.cargar();
    });
  }

  eliminar(cliente: Cliente): void {
    if (!confirm(`¿Eliminar a ${cliente.nombreCompleto}?`)) return;
    this.http.delete(`/api/clientes/${cliente.id}`).subscribe(() => {
      this.avisos.ok('Cliente eliminado.');
      this.cargar();
    });
  }

  elegirArchivo(evento: Event): void {
    this.archivo = (evento.target as HTMLInputElement).files?.[0] ?? null;
  }

  cargarArchivo(): void {
    if (!this.archivo) {
      this.avisos.error('Seleccione un archivo .xlsx o .csv.');
      return;
    }
    const data = new FormData();
    data.append('archivo', this.archivo);
    this.http.post<CargaMasiva>('/api/clientes/carga-masiva', data).subscribe(resultado => {
      this.resultado = resultado;
      this.avisos.ok(`Creados: ${resultado.creados}. Fallidos: ${resultado.fallidos}.`);
      this.cargar();
    });
  }

  plantilla(): void {
    this.http.get('/api/clientes/plantilla', { responseType: 'blob' }).subscribe(blob => descargar(blob, 'plantilla-clientes.xlsx'));
  }
}
