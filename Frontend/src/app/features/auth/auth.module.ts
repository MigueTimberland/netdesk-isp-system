import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { invitadoGuard } from '../../core/guardianes';
import { SharedModule } from '../../shared/shared.module';
import { LoginComponent } from './login.component';
import { RecuperarComponent } from './recuperar.component';

const routes: Routes = [
  { path: 'login', component: LoginComponent, canActivate: [invitadoGuard] },
  { path: 'recuperar', component: RecuperarComponent, canActivate: [invitadoGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'login' }
];

@NgModule({
  declarations: [LoginComponent, RecuperarComponent],
  imports: [SharedModule, RouterModule.forChild(routes)]
})
export class AuthModule {}
