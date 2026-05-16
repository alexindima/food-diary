import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { ContentReport, CreateReportDto } from '../models/report.data';
import { ReportService } from './report.service';

const BASE_URL = environment.apiUrls.reports;

let service: ReportService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [ReportService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ReportService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('ReportService', () => {
    it('creates content report', () => {
        const dto: CreateReportDto = { targetType: 'Recipe', targetId: 'recipe-1', reason: 'Spam' };
        const report = createReport(dto);

        service.create(dto).subscribe(result => {
            expect(result).toEqual(report);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(dto);
        req.flush(report);
    });
});

function createReport(dto: CreateReportDto): ContentReport {
    return {
        id: 'report-1',
        reporterId: 'user-1',
        targetType: dto.targetType,
        targetId: dto.targetId,
        reason: dto.reason,
        status: 'Pending',
        adminNote: null,
        createdAtUtc: '2026-05-16T10:00:00.000Z',
        reviewedAtUtc: null,
    };
}
