import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { AggregatedStatistics } from '../models/statistics.data';
import { StatisticsService } from './statistics.service';

const BASE_URL = environment.apiUrls.statistics;
const SERVICE_URL = `${BASE_URL}/`;
const QUANTIZATION_DAYS = 7;
const RESPONSE: AggregatedStatistics[] = [
    {
        dateFrom: new Date('2026-05-01T00:00:00.000Z'),
        dateTo: new Date('2026-05-07T23:59:59.999Z'),
        totalCalories: 1800,
        averageProteins: 120,
        averageFats: 70,
        averageCarbs: 160,
        averageFiber: 20,
    },
];

let service: StatisticsService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [StatisticsService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(StatisticsService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('StatisticsService', () => {
    it('queries aggregated statistics with ISO date params', () => {
        const dateFrom = new Date('2026-05-01T00:00:00.000Z');
        const dateTo = new Date('2026-05-07T23:59:59.999Z');

        service.getAggregatedStatistics({ dateFrom, dateTo, quantizationDays: QUANTIZATION_DAYS }).subscribe(result => {
            expect(result).toEqual(RESPONSE);
        });

        const req = httpMock.expectOne(request => request.url === SERVICE_URL);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('dateFrom')).toBe(dateFrom.toISOString());
        expect(req.request.params.get('dateTo')).toBe(dateTo.toISOString());
        expect(req.request.params.get('quantizationDays')).toBe(String(QUANTIZATION_DAYS));
        req.flush(RESPONSE);
    });

    it('normalizes string date params to ISO strings', () => {
        service
            .getAggregatedStatistics({
                dateFrom: '2026-05-01',
                dateTo: '2026-05-07',
            })
            .subscribe();

        const req = httpMock.expectOne(request => request.url === SERVICE_URL);
        expect(req.request.params.get('dateFrom')).toBe(new Date('2026-05-01').toISOString());
        expect(req.request.params.get('dateTo')).toBe(new Date('2026-05-07').toISOString());
        req.flush([]);
    });
});
