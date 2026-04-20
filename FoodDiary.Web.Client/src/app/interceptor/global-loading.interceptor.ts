import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, finalize } from 'rxjs';
import { FORCE_GLOBAL_LOADING, SKIP_GLOBAL_LOADING } from '../constants/global-loading-context.tokens';
import { GlobalLoadingService } from '../services/global-loading.service';

@Injectable()
export class GlobalLoadingInterceptor implements HttpInterceptor {
    private readonly globalLoadingService = inject(GlobalLoadingService);

    public intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
        if (!this.shouldTrack(req)) {
            return next.handle(req);
        }

        const complete = this.globalLoadingService.trackRequest();

        return next.handle(req).pipe(
            finalize(() => {
                complete();
            }),
        );
    }

    private shouldTrack(req: HttpRequest<unknown>): boolean {
        if (req.context.get(SKIP_GLOBAL_LOADING)) {
            return false;
        }

        return req.method === 'GET' || req.context.get(FORCE_GLOBAL_LOADING);
    }
}
