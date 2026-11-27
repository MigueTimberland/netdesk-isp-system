import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Recuperacion } from '../../core/modelos';
import { NotificacionService } from '../../core/servicios';
import { correoPattern, passwordPattern } from '../../core/validadores';

@Component({
  selector: 'app-recuperar',
  templateUrl: './recuperar.component.html',
  standalone: false
})
export class RecuperarComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private avisos = inject(NotificacionService);
  private router = inject(Router);
  paso: 'correo' | 'codigo' = 'correo';
  codigoDemo: string | null = null;
  solicitar = this.fb.nonNullable.group({
    correo: ['', [Validators.required, Validators.pattern(correoPattern)]]
  });
  restablecer = this.fb.nonNullable.group({
    codigo: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(8)]],
    nuevaPassword: ['', [Validators.required, Validators.pattern(passwordPattern)]]
  });

  invalido(form: 'solicitar' | 'restablecer', campo: string): boolean {
    const grupo = (form === 'solicitar' ? this.solicitar : this.restablecer) as FormGroup;
    const control = grupo.get(campo);
    return !!control && control.touched && control.invalid;
  }

  pedir(): void {
    if (this.solicitar.invalid) {
      this.solicitar.markAllAsTouched();
      return;
    }
    this.http.post<Recuperacion>('/api/auth/recuperar', this.solicitar.getRawValue()).subscribe(respuesta => {
      this.codigoDemo = respuesta.codigoDemostracion;
      this.paso = 'codigo';
      this.avisos.ok(respuesta.mensaje);
    });
  }

  guardar(): void {
    if (this.restablecer.invalid) {
      this.restablecer.markAllAsTouched();
      return;
    }
    const cuerpo = {
      correo: this.solicitar.getRawValue().correo,
      ...this.restablecer.getRawValue()
    };
    this.http.post('/api/auth/restablecer', cuerpo).subscribe(() => {
      this.avisos.ok('Contraseña actualizada. Ya puede ingresar.');
      this.router.navigate(['/auth/login']);
    });
  }
}
