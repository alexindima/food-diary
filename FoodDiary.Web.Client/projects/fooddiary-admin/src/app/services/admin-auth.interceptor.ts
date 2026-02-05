import { HttpInterceptorFn } from '@angular/common/http';

export const adminAuthInterceptor: HttpInterceptorFn = (req, next) => {
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
  );
};
