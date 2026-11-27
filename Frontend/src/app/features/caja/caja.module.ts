import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { CajaComponent } from './caja.component';

const routes: Routes = [{ path: '', component: CajaComponent }];

@NgModule({
  declarations: [CajaComponent],
  imports: [SharedModule, FormsModule, RouterModule.forChild(routes)]
})
export class CajaModule {}
