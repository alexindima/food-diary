import { inject, Injectable } from '@angular/core';
import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { AuthService } from '../services/auth.service';
import { catchError, Observable, switchMap, throwError } from 'rxjs';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    private readonly authService = inject(AuthService);

    public intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        const token = this.authService.getToken();
        let request = req;

        if (token) {
            request = req.clone({
                headers: req.headers.set('Authorization', `Bearer ${token}`),
            });
        }

        return next.handle(request).pipe(
            catchError((error: HttpErrorResponse) => {
                if (error.status === 401 && this.isTokenExpiredError(error)) {
                    return this.authService.refreshToken().pipe(
                        switchMap(newAuthResponse => {
                            if (newAuthResponse) {
                                const newRequest = req.clone({
                                    headers: req.headers.set('Authorization', `Bearer ${newAuthResponse.accessToken}`),
                                });
                                return next.handle(newRequest);
                            }
                            return throwError(() => error);
                        }),
                    );
                }

                return throwError(() => error);
            }),
        );
    }

    private isTokenExpiredError(error: HttpErrorResponse): boolean {
        return error.error?.data?.message === 'Token has expired';
    }
}
