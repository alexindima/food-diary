import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { GoalsService } from './goals.service';
import { environment } from '../../../../environments/environment';

describe('GoalsService', () => {
    let service: GoalsService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.goals;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                GoalsService,
                provideHttpClient(),
                provideHttpClientTesting(),
            ],
        });

        service = TestBed.inject(GoalsService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should get goals', () => {
        const mockGoals = { calories: 2000, protein: 150 };

        service.getGoals().subscribe(result => {
            expect(result).toEqual(mockGoals as any);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('GET');
        req.flush(mockGoals);
    });

    it('should return null on getGoals error', () => {
        service.getGoals().subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('should update goals', () => {
        const request = { calories: 2500, protein: 180 };
        const mockResponse = { calories: 2500, protein: 180 };

        service.updateGoals(request as any).subscribe(result => {
            expect(result).toEqual(mockResponse as any);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(request);
        req.flush(mockResponse);
    });

    it('should return null on updateGoals error', () => {
        const request = { calories: 2500 };

        service.updateGoals(request as any).subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });
});
