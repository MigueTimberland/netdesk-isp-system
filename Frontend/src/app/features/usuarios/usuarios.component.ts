import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Catalogo, CargaMasiva, Usuario } from '../../core/modelos';
import { NotificacionService } from '../../core/servicios';
import { correoPattern, identificacionPattern, nombrePattern, passwordPattern, telefonoPattern, usuarioPattern } from '../../core/validadores';

@Component({
  selector: 'app-usuarios',
  templateUrl: './usuarios.component.html',
  standalone: false
})
export class UsuariosComponent implements OnInit {
  usuarios: Usuario[] = [];
  roles: Catalogo[] = [];
  editando: Usuario | null = null;
  archivo: File | null = null;
  resultado: CargaMasiva | null = null;
  private fb = inject(FormBuilder);
  form = this.fb.nonNullable.group({
    nombreUsuario: ['', [Validators.required, Validators.pattern(usuarioPattern)]],
    password: ['', [Validators.required, Validators.pattern(passwordPattern)]],
    nombres: ['', [Validators.required, Validators.pattern(nombrePattern)]],
    apellidos: ['', [Validators.required, Validators.pattern(nombrePattern)]],
    identificacion: ['', [Validators.required, Validators.pattern(identificacionPattern)]],
    correo: ['', [Validators.required, Validators.pattern(correoPattern)]],
    telefono: ['', [Validators.required, Validators.pattern(telefonoPattern)]],
    rolId: [0, Validators.min(1)]
  });

  constructor(private http: HttpClient, private avisos: NotificacionService) {}

  ngOnInit(): void {
    this.http.get<Catalogo[]>('/api/catalogos/roles').subscribe(roles => this.roles = roles);
    this.cargar();
  }

  cargar(): void {
    this.http.get<Usuario[]>('/api/usuarios').subscribe(usuarios => this.usuarios = usuarios);
  }

  invalido(campo: string): boolean {
    const control = this.form.get(campo);
    return !!control && control.touched && control.invalid;
  }

  nuevo(): void {
    this.editando = null;
    this.form.reset({ rolId: 0, nombreUsuario: '', password: '', nombres: '', apellidos: '', identificacion: '', correo: '', telefono: '' });
    this.form.get('password')?.setValidators([Validators.required, Validators.pattern(passwordPattern)]);
    this.form.get('password')?.updateValueAndValidity();
  }

  editar(usuario: Usuario): void {
    this.editando = usuario;
    this.form.patchValue({ ...usuario, password: '' });
    this.form.get('password')?.setValidators([Validators.pattern(passwordPattern)]);
    this.form.get('password')?.updateValueAndValidity();
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const valor = this.form.getRawValue();
    const peticion = this.editando
      ? this.http.put(`/api/usuarios/${this.editando.id}`, { ...valor, password: valor.password || null })
      : this.http.post('/api/usuarios', valor);
    peticion.subscribe(() => {
      this.avisos.ok(this.editando ? 'Usuario actualizado.' : 'Usuario creado.');
      this.nuevo();
      this.cargar();
    });
  }

  estado(usuario: Usuario, estado: string): void {
    this.http.patch(`/api/usuarios/${usuario.id}/estado`, { estado }).subscribe(() => {
      this.avisos.ok('Estado actualizado.');
      this.cargar();
    });
  }

  aprobar(usuario: Usuario, aprobar: boolean): void {
    const ruta = aprobar ? 'aprobar' : 'rechazar';
    this.http.post(`/api/usuarios/${usuario.id}/${ruta}`, {}).subscribe(() => {
      this.avisos.ok(aprobar ? 'Usuario aprobado.' : 'Usuario rechazado.');
      this.cargar();
    });
  }

  eliminar(usuario: Usuario): void {
    if (!confirm(`¿Eliminar a ${usuario.nombreUsuario}?`)) {
      return;
    }
    this.http.delete(`/api/usuarios/${usuario.id}`).subscribe(() => {
      this.avisos.ok('Usuario eliminado.');
      this.cargar();
    });
  }

  elegirArchivo(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    this.archivo = input.files?.[0] ?? null;
  }

  cargarArchivo(): void {
    if (!this.archivo) {
      this.avisos.error('Seleccione un archivo .xlsx o .csv.');
      return;
    }
    const data = new FormData();
    data.append('archivo', this.archivo);
    this.http.post<CargaMasiva>('/api/usuarios/carga-masiva', data).subscribe(resultado => {
      this.resultado = resultado;
      this.avisos.ok(`Creados: ${resultado.creados}. Fallidos: ${resultado.fallidos}.`);
      this.cargar();
    });
  }

  plantilla(): void {
    this.http.get('/api/usuarios/plantilla', { responseType: 'blob' }).subscribe(blob => descargar(blob, 'plantilla-usuarios.xlsx'));
  }
}

export function descargar(blob: Blob, nombre: string): void {
  const url = URL.createObjectURL(blob);
  const enlace = document.createElement('a');
  enlace.href = url;
  enlace.download = nombre;
  enlace.click();
  URL.revokeObjectURL(url);
}
