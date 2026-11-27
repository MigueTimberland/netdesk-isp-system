import { Pipe, PipeTransform } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { mensajeControl } from '../core/validadores';

@Pipe({ name: 'errorCampo', standalone: false })
export class ErrorCampoPipe implements PipeTransform {
  transform(control: AbstractControl | null): string {
    return mensajeControl(control);
  }
}
