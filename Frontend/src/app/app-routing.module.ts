import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/guardianes';
import { ShellComponent } from './shared/shell.component';

const routes: Routes = [
  { path: 'auth', loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule) },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: 'bienvenida', loadChildren: () => import('./features/bienvenida/bienvenida.module').then(m => m.BienvenidaModule) },
      { path: 'dashboard', canActivate: [roleGuard], data: { roles: ['Administrador'] }, loadChildren: () => import('./features/dashboard/dashboard.module').then(m => m.DashboardModule) },
      { path: 'usuarios', canActivate: [roleGuard], data: { roles: ['Administrador'] }, loadChildren: () => import('./features/usuarios/usuarios.module').then(m => m.UsuariosModule) },
      { path: 'servicios', canActivate: [roleGuard], data: { roles: ['Administrador'] }, loadChildren: () => import('./features/servicios/servicios.module').then(m => m.ServiciosModule) },
      { path: 'turnos', canActivate: [roleGuard], data: { roles: ['Gestor'] }, loadChildren: () => import('./features/turnos/turnos.module').then(m => m.TurnosModule) },
      { path: 'caja', canActivate: [roleGuard], data: { roles: ['Cajero'] }, loadChildren: () => import('./features/caja/caja.module').then(m => m.CajaModule) },
      { path: 'clientes', canActivate: [roleGuard], data: { roles: ['Cajero'] }, loadChildren: () => import('./features/clientes/clientes.module').then(m => m.ClientesModule) },
      { path: '', pathMatch: 'full', redirectTo: 'bienvenida' }
    ]
  },
  { path: '**', redirectTo: 'bienvenida' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
