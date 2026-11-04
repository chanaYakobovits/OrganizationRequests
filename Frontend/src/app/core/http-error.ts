import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../models/request.models';

export function getErrorStatus(err: unknown): number | null {
  return err instanceof HttpErrorResponse ? err.status : null;
}

export function getErrorMessage(err: unknown): string {
  if (!(err instanceof HttpErrorResponse)) return 'אירעה שגיאה לא צפויה.';

  
  if (err.status === 0) return 'לא ניתן להתחבר לשרת. ודאי שה-API רץ.';

  const problem = err.error as ProblemDetails | null;

 
  if (problem?.errors) {
    const first = Object.values(problem.errors).flat()[0];
    if (first) return first;
  }
  if (problem?.detail) return problem.detail;

  return 'אירעה שגיאה בשרת. נסי שוב.';
}