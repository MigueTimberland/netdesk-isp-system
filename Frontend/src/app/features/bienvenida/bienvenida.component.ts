import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Bienvenida } from '../../core/modelos';

@Component({
  selector: 'app-bienvenida',
  templateUrl: './bienvenida.component.html',
  standalone: false
})
export class BienvenidaComponent implements OnInit {
  datos?: Bienvenida;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get<Bienvenida>('/api/dashboard/bienvenida').subscribe(datos => this.datos = datos);
  }
}
