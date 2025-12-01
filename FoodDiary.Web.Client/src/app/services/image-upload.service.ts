import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { ImageUploadUrlResponse } from '../types/image-upload.data';
import { SKIP_AUTH } from '../constants/http-context.tokens';

@Injectable({
    providedIn: 'root',
})
export class ImageUploadService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = environment.apiUrls.images;

    public requestUploadUrl(file: File): Observable<ImageUploadUrlResponse> {
        const body = {
            fileName: file.name,
            contentType: file.type,
            fileSizeBytes: file.size,
        };

        return this.http.post<ImageUploadUrlResponse>(`${this.baseUrl}/upload-url`, body).pipe(
            catchError(error => {
                console.error('Failed to request image upload URL', error);
                return throwError(() => error);
            }),
        );
    }

    public uploadToPresignedUrl(uploadUrl: string, file: File): Observable<void> {
        const headers = new HttpHeaders({
            'Content-Type': file.type,
        });

        const context = new HttpContext().set(SKIP_AUTH, true);

        return this.http.put(uploadUrl, file, { headers, responseType: 'text', context }).pipe(
            map(() => void 0),
            catchError(error => {
                console.error('Failed to upload image to S3', error);
                return throwError(() => error);
            }),
        );
    }
}
