import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Menu, UsuarioSesion } from '../core/modelos';
import { AuthService } from '../core/servicios';

@Component({
  selector: 'app-shell',
  templateUrl: './shell.component.html',
  standalone: false
})
export class ShellComponent implements OnInit {
  menus: Menu[] = [];
  usuario: UsuarioSesion | null = null;

  constructor(private http: HttpClient, private auth: AuthService) {}

  ngOnInit(): void {
    this.usuario = this.auth.usuario;
    this.auth.refrescar().subscribe(usuario => this.usuario = usuario);
    this.http.get<Menu[]>('/api/menus/mios').subscribe(menus => this.menus = menus);
  }

  salir(): void {
    this.auth.logout();
  }
}
