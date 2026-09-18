import { type HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { AdminAiPromptsService } from './admin-ai-prompts.service';

const testAuthInterceptor: HttpInterceptorFn = (request, next) =>
    next(request.clone({ setHeaders: { Authorization: 'Bearer test-only' } }));

describe('AdminAiPromptsService', () => {
    beforeEach(() =>
        TestBed.configureTestingModule({
            providers: [provideHttpClient(withInterceptors([testAuthInterceptor])), provideHttpClientTesting()],
        }),
    );
    afterEach(() => {
        TestBed.inject(HttpTestingController).verify();
    });

    it('uploads photo bytes without forwarding the admin bearer token to the signed storage URL', () => {
        const service = TestBed.inject(AdminAiPromptsService);
        const http = TestBed.inject(HttpTestingController);
        const file = new File(['image bytes'], 'meal.png', { type: 'image/png' });
        let result: string | undefined;
        service.uploadImage(file).subscribe(value => {
            result = value.assetId;
        });
        const request = http.expectOne('http://localhost:5300/api/v1/images/upload-url');
        expect(request.request.headers.has('Authorization')).toBe(true);
        request.flush({ uploadUrl: 'https://storage.example.test/signed', assetId: 'asset' });
        const upload = http.expectOne('https://storage.example.test/signed');
        expect(upload.request.headers.has('Authorization')).toBe(false);
        expect(upload.request.body).toBe(file);
        upload.flush('');
        const confirm = http.expectOne('http://localhost:5300/api/v1/images/asset/confirm');
        expect(confirm.request.headers.has('Authorization')).toBe(true);
        confirm.flush({ assetId: 'asset' });
        expect(result).toBe('asset');
    });
});
