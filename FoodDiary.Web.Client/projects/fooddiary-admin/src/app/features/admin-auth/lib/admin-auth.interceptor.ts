import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const adminAuthInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const token = localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
  if (!token) {
    return next(req);
  }

  return next(
    req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    })
  ).pipe(
    catchError((error: HttpErrorResponse) => {
      if (req.url.includes('/admin-sso/exchange')) {
        return throwError(() => error);
      }

      if (error.status === 401 || error.status === 403) {
        localStorage.removeItem('authToken');
        localStorage.removeItem('refreshToken');
        sessionStorage.removeItem('authToken');

        const reason = error.status === 403 ? 'forbidden' : 'unauthenticated';
        const returnUrl = router.url;
        router.navigate(['/unauthorized'], {
          queryParams: { reason, returnUrl },
        });
      }

      return throwError(() => error);
    })
  );
};
