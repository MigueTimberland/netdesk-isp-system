import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { TurnosComponent } from './turnos.component';

const routes: Routes = [{ path: '', component: TurnosComponent }];

@NgModule({
  declarations: [TurnosComponent],
  imports: [SharedModule, RouterModule.forChild(routes)]
})
export class TurnosModule {}
