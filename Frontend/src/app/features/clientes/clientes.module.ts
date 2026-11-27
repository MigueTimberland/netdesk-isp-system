import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { ClientesComponent } from './clientes.component';

const routes: Routes = [{ path: '', component: ClientesComponent }];

@NgModule({
  declarations: [ClientesComponent],
  imports: [SharedModule, FormsModule, RouterModule.forChild(routes)]
})
export class ClientesModule {}
