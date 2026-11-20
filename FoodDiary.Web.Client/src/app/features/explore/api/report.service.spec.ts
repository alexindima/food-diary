import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { ApiErrorHttpResponse } from '../../../shared/api/sdk/generated/model/api-error-http-response';
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

    it('preserves the duplicate report conflict for the selected comment', async () => {
        const commentId = entityId<'recipe-comment'>('524059ff-3cd3-4495-9f33-f440d03d03fb');
        const pending = firstValueFrom(service.create({ target: { kind: 'comment', commentId }, reason: 'Spam' }));
        const duplicate = { error: 'ContentReport.AlreadyReported' } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne({ method: 'POST', url: BASE_URL });
        expect(request.request.body).toEqual({ targetType: 'Comment', targetId: commentId, reason: 'Spam' });
        request.flush(duplicate, { status: HttpStatusCode.Conflict, statusText: 'Conflict' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.Conflict, error: duplicate });
    });
});
