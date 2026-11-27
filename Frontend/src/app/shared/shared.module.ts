import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterModule } from '@angular/router';
import { ErrorCampoPipe } from './error-campo.pipe';
import { ShellComponent } from './shell.component';

const material = [
  MatButtonModule,
  MatFormFieldModule,
  MatIconModule,
  MatInputModule,
  MatProgressBarModule,
  MatSelectModule,
  MatTooltipModule
];

@NgModule({
  declarations: [ShellComponent, ErrorCampoPipe],
  imports: [CommonModule, RouterModule, ReactiveFormsModule, ...material],
  exports: [CommonModule, RouterModule, ReactiveFormsModule, ErrorCampoPipe, ShellComponent, ...material]
})
export class SharedModule {}
