import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const usuarioPattern = /^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,20}$/;
export const passwordPattern = /^(?=.*[A-Z])(?=.*\d).{8,30}$/;
export const identificacionPattern = /^\d{10,13}$/;
export const telefonoPattern = /^09\d{8,13}$/;
export const nombrePattern = /^[A-Za-zÁÉÍÓÚÜáéíóúüÑñ ]{2,80}$/;
export const correoPattern = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

export function longitudEntre(min: number, max: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const valor = (control.value ?? '').toString().trim();
    if (!valor) {
      return null;
    }
    return valor.length >= min && valor.length <= max ? null : { longitud: { min, max } };
  };
}

export function mensajeControl(control: AbstractControl | null): string {
  if (!control || !control.errors) {
    return '';
  }
  const errores = control.errors;
  if (errores['required']) return 'Este campo es obligatorio.';
  if (errores['minlength']) return `Mínimo ${errores['minlength'].requiredLength} caracteres.`;
  if (errores['maxlength']) return `Máximo ${errores['maxlength'].requiredLength} caracteres.`;
  if (errores['min']) return `El valor mínimo es ${errores['min'].min}.`;
  if (errores['max']) return `El valor máximo es ${errores['max'].max}.`;
  if (errores['longitud']) return `Debe tener entre ${errores['longitud'].min} y ${errores['longitud'].max} caracteres.`;
  if (errores['pattern']) return 'El formato no cumple la regla indicada.';
  return 'El dato no es válido.';
}
