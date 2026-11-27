import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { UsuariosComponent } from './usuarios.component';

const routes: Routes = [{ path: '', component: UsuariosComponent }];

@NgModule({
  declarations: [UsuariosComponent],
  imports: [SharedModule, RouterModule.forChild(routes)]
})
export class UsuariosModule {}
