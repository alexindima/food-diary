import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { FoodVisionRequest, FoodVisionResponse } from '../types/ai.data';

@Injectable({ providedIn: 'root' })
export class AiFoodService {
    private readonly baseUrl = environment.apiUrls.ai;

    public constructor(private readonly http: HttpClient) {}

    public analyzeFoodImage(request: FoodVisionRequest): Observable<FoodVisionResponse> {
        return this.http.post<FoodVisionResponse>(`${this.baseUrl}/food/vision`, request);
    }
}
