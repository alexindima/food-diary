import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { MarketingAttributionSummary } from '../models/admin-acquisition.data';
import { AdminAcquisitionService, DEFAULT_ACQUISITION_WINDOW_HOURS } from './admin-acquisition.service';

describe('AdminAcquisitionService', () => {
    const SELECTED_WINDOW_HOURS = 168;
    let service: AdminAcquisitionService;
    let httpMock: HttpTestingController;

    const summaryUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/acquisition/summary`;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [AdminAcquisitionService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(AdminAcquisitionService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('requests the default reporting window', () => {
        service.getSummary().subscribe();

        const request = httpMock.expectOne(
            candidate => candidate.url === summaryUrl && candidate.params.get('hours') === DEFAULT_ACQUISITION_WINDOW_HOURS.toString(),
        );
        expect(request.request.method).toBe('GET');
        request.flush({});
    });

    it('requests a selected reporting window', () => {
        service.getSummary(SELECTED_WINDOW_HOURS).subscribe();

        const request = httpMock.expectOne(
            candidate => candidate.url === summaryUrl && candidate.params.get('hours') === SELECTED_WINDOW_HOURS.toString(),
        );
        expect(request.request.method).toBe('GET');
        request.flush({});
    });

    it('keeps visit metrics usable while an older API version is still serving requests', () => {
        let result: MarketingAttributionSummary | undefined;
        service.getSummary().subscribe(summary => {
            result = summary;
        });

        const request = httpMock.expectOne(candidate => candidate.url === summaryUrl);
        request.flush({ visits: 52 });

        expect(result).toMatchObject({
            attributedVisits: 0,
            organicVisits: 52,
        });
    });
});
