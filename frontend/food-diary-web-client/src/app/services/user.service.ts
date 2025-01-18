import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';
import { catchError, map, Observable, of } from 'rxjs';
import { ApiResponse } from '../types/api-response.data';
import { UpdateUserDto, User } from '../types/user.data';

@Injectable({
    providedIn: 'root',
})
export class UserService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.users;

    public getUserCalories(): Observable<number | null> {
        return this.getInfo().pipe(
            map(response => response.data?.calories || null)
        );
    }

    public getInfo(): Observable<ApiResponse<User | null>> {
        return this.get<ApiResponse<User>>('info').pipe(catchError(error => of(ApiResponse.error(error.error?.error, null))));
    }

    public update(data: UpdateUserDto): Observable<ApiResponse<User | null>> {
        return this.patch<ApiResponse<User>>('', data).pipe(catchError(error => of(ApiResponse.error(error.error?.error, null))));
    }
}
