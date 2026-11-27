import { Component } from '@angular/core';
import { LoadingService } from './core/servicios';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: false
})
export class AppComponent {
  constructor(public loading: LoadingService) {}
}
