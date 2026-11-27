import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, finalize, throwError } from 'rxjs';
import { AuthService, LoadingService, NotificacionService } from './servicios';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  constructor(private auth: AuthService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.auth.token;
    if (!token || !req.url.startsWith('/api')) {
      return next.handle(req);
    }
    return next.handle(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
  }
}

@Injectable()
export class LoadingInterceptor implements HttpInterceptor {
  constructor(private loading: LoadingService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (!req.url.startsWith('/api')) {
      return next.handle(req);
    }
    this.loading.mostrar();
    return next.handle(req).pipe(finalize(() => this.loading.ocultar()));
  }
}

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(private notificacion: NotificacionService, private auth: AuthService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (req.headers.has('X-Silenciar')) {
          return throwError(() => error);
        }
        if (error.status === 401 && !req.url.includes('/api/auth/login') && !req.url.includes('/api/auth/restablecer')) {
          this.auth.salir();
        }
        this.notificacion.error(this.detalle(error));
        return throwError(() => error);
      })
    );
  }

  private detalle(error: HttpErrorResponse): string {
    const cuerpo = error.error;
    if (cuerpo && typeof cuerpo === 'object') {
      const errores = cuerpo.errores as Record<string, string[]> | undefined;
      if (errores) {
        const textos = Object.values(errores).flat();
        if (textos.length) {
          return textos.join(' ');
        }
      }
      if (cuerpo.mensaje) {
        return cuerpo.mensaje;
      }
    }
    if (error.status === 0) {
      return 'No hay conexión con el servidor.';
    }
    return 'No se pudo completar la solicitud.';
  }
}
