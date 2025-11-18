import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

@Injectable()
export class LoggingApiService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.logs;

    public logError(payload: unknown): Observable<void> {
        return this.post<void>('', payload);
    }
}
