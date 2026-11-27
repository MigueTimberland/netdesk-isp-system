import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { BehaviorSubject, tap } from 'rxjs';
import { LoginResponse, UsuarioSesion } from './modelos';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private activos = 0;
  private estado = new BehaviorSubject(false);
  readonly visible$ = this.estado.asObservable();

  mostrar(): void {
    this.activos++;
    this.estado.next(true);
  }

  ocultar(): void {
    this.activos = Math.max(0, this.activos - 1);
    this.estado.next(this.activos > 0);
  }
}

@Injectable({ providedIn: 'root' })
export class NotificacionService {
  constructor(private snack: MatSnackBar) {}

  ok(mensaje: string): void {
    this.snack.open(mensaje, 'Cerrar', { duration: 4000, panelClass: 'aviso-ok' });
  }

  error(mensaje: string): void {
    this.snack.open(mensaje, 'Cerrar', { duration: 6000, panelClass: 'aviso-error' });
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private claveToken = 'netdesk_token';
  private claveUsuario = 'netdesk_usuario';
  private usuarioEstado = new BehaviorSubject<UsuarioSesion | null>(this.leer());
  readonly usuario$ = this.usuarioEstado.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  get token(): string | null {
    return localStorage.getItem(this.claveToken);
  }

  get usuario(): UsuarioSesion | null {
    return this.usuarioEstado.value;
  }

  get autenticado(): boolean {
    return !!this.token;
  }

  login(nombreUsuario: string, password: string) {
    return this.http.post<LoginResponse>('/api/auth/login', { nombreUsuario, password }).pipe(
      tap(respuesta => this.guardar(respuesta))
    );
  }

  refrescar() {
    return this.http.get<UsuarioSesion>('/api/auth/me').pipe(
      tap(usuario => {
        localStorage.setItem(this.claveUsuario, JSON.stringify(usuario));
        this.usuarioEstado.next(usuario);
      })
    );
  }

  logout(): void {
    this.http.post('/api/auth/logout', {}).subscribe({ next: () => this.salir(), error: () => this.salir() });
  }

  salir(): void {
    localStorage.removeItem(this.claveToken);
    localStorage.removeItem(this.claveUsuario);
    this.usuarioEstado.next(null);
    this.router.navigate(['/auth/login']);
  }

  private guardar(respuesta: LoginResponse): void {
    localStorage.setItem(this.claveToken, respuesta.token);
    localStorage.setItem(this.claveUsuario, JSON.stringify(respuesta.usuario));
    this.usuarioEstado.next(respuesta.usuario);
  }

  private leer(): UsuarioSesion | null {
    const crudo = localStorage.getItem(this.claveUsuario);
    if (!crudo || !localStorage.getItem(this.claveToken)) {
      return null;
    }
    try {
      return JSON.parse(crudo) as UsuarioSesion;
    } catch {
      return null;
    }
  }
}
