import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { ContentReportHttpResponse } from '../../../shared/api/sdk/generated/model/content-report-http-response';
import { entityId } from '../../../shared/models/semantics/entity-id';
import type { CreateReportDto } from '../models/report.data';
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
    it.each([
        { target: { kind: 'recipe', recipeId: entityId<'recipe'>('recipe-1') }, targetType: 'Recipe', targetId: 'recipe-1' },
        { target: { kind: 'comment', commentId: entityId<'recipe-comment'>('comment-1') }, targetType: 'Comment', targetId: 'comment-1' },
    ] as const)('creates a $targetType report using the unchanged scalar HTTP body', ({ target, targetType, targetId }) => {
        const dto: CreateReportDto = { target, reason: 'Spam' };
        const report: ContentReportHttpResponse = {
            id: 'report-1',
            reporterId: 'user-1',
            targetType,
            targetId,
            reason: 'Spam',
            status: 'Pending',
            adminNote: null,
            createdAtUtc: '2026-05-16T10:00:00.000Z',
            reviewedAtUtc: null,
        };

        service.create(dto).subscribe(result => {
            const { targetType: _type, targetId: _id, ...fields } = report;
            expect(result).toEqual({ ...fields, target });
        });

        const req = httpMock.expectOne(BASE_URL);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ targetType, targetId, reason: 'Spam' });
        req.flush(report);
    });
});
