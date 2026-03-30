import { TestBed } from '@angular/core/testing';
import { HttpClient, HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RetryInterceptor } from './retry.interceptor';

describe('RetryInterceptor', () => {
    let http: HttpClient;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                {
                    provide: HTTP_INTERCEPTORS,
                    useClass: RetryInterceptor,
                    multi: true,
                },
                provideHttpClient(withInterceptorsFromDi()),
                provideHttpClientTesting(),
            ],
        });

        http = TestBed.inject(HttpClient);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should pass through successful requests', () => {
        http.get('/api/test').subscribe(response => {
            expect(response).toEqual({ data: 'ok' });
        });

        const req = httpMock.expectOne('/api/test');
        req.flush({ data: 'ok' });
    });

    it('should not retry on 400 errors', () => {
        http.get('/api/test').subscribe({
            error: error => {
                expect(error.status).toBe(400);
            },
        });

        const req = httpMock.expectOne('/api/test');
        req.flush('Bad Request', { status: 400, statusText: 'Bad Request' });
    });

    it('should not retry on 404 errors', () => {
        http.get('/api/test').subscribe({
            error: error => {
                expect(error.status).toBe(404);
            },
        });

        const req = httpMock.expectOne('/api/test');
        req.flush('Not Found', { status: 404, statusText: 'Not Found' });
    });

    it('should not retry POST requests', () => {
        http.post('/api/test', { value: 1 }).subscribe({
            error: error => {
                expect(error.status).toBe(500);
            },
        });

        const req = httpMock.expectOne('/api/test');
        req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
    });
});
