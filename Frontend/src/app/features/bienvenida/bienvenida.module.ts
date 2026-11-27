import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { BienvenidaComponent } from './bienvenida.component';

const routes: Routes = [{ path: '', component: BienvenidaComponent }];

@NgModule({
  declarations: [BienvenidaComponent],
  imports: [SharedModule, RouterModule.forChild(routes)]
})
export class BienvenidaModule {}
