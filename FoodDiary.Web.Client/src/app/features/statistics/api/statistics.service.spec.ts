import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { AggregatedStatistics } from '../models/statistics.data';
import { StatisticsService } from './statistics.service';

const BASE_URL = environment.apiUrls.statistics;
const SERVICE_URL = BASE_URL;
const SUMMARY_URL = `${BASE_URL}/summary`;
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
        totalProteins: 120,
        totalFats: 70,
        totalCarbs: 160,
        totalFiber: 20,
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

describe('StatisticsService summaries', () => {
    it('queries chart buckets with local instants and exact measurements with calendar dates', () => {
        const dateFrom = new Date('2026-05-01T00:00:00.000Z');
        const dateTo = new Date('2026-05-07T23:59:59.999Z');
        const response = { nutrition: RESPONSE, weight: [], waist: [] };

        const params = {
            dateFrom,
            dateTo,
            quantizationDays: QUANTIZATION_DAYS,
            bodyDateFrom: '2026-05-01',
            bodyDateTo: '2026-05-07',
            timeZoneId: 'Asia/Tbilisi',
        };
        service.getSummary(params).subscribe(result => {
            expect(result).toEqual(response);
        });
        service.getBodyMeasurements(params).subscribe(result => {
            expect(result).toEqual({
                weightMeasurements: { first: 80, last: 79, count: 2 },
                waistMeasurements: { first: 90, last: 90, count: 1 },
            });
        });

        const req = httpMock.expectOne(request => request.url === SUMMARY_URL);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('dateFrom')).toBe(dateFrom.toISOString());
        expect(req.request.params.get('dateTo')).toBe(dateTo.toISOString());
        expect(req.request.params.get('quantizationDays')).toBe(String(QUANTIZATION_DAYS));
        expect(req.request.params.get('bodyDateFrom')).toBe('2026-05-01');
        expect(req.request.params.get('bodyDateTo')).toBe('2026-05-07');
        expect(req.request.params.get('timeZoneId')).toBe('Asia/Tbilisi');
        req.flush(response);
        const weight = httpMock.expectOne(request => request.url === environment.apiUrls.weights);
        expect(weight.request.params.get('dateFrom')).toBe('2026-05-01');
        expect(weight.request.params.get('dateTo')).toBe('2026-05-07');
        expect(weight.request.params.get('limit')).toBe('500');
        expect(weight.request.params.get('sort')).toBe('asc');
        weight.flush([{ weightKg: 80 }, { weightKg: 79 }]);
        const waist = httpMock.expectOne(request => request.url === environment.apiUrls.waists);
        expect(waist.request.params.get('dateFrom')).toBe('2026-05-01');
        expect(waist.request.params.get('dateTo')).toBe('2026-05-07');
        waist.flush([{ circumferenceCm: 90 }]);
    });

    it('keeps the summary available when an exact measurement read fails', async () => {
        const params = { dateFrom: '2026-05-01', dateTo: '2026-05-07' };
        const summary = firstValueFrom(service.getSummary(params));
        const measurements = firstValueFrom(service.getBodyMeasurements(params));
        const failed = expect(measurements).rejects.toMatchObject({ status: 500 });
        const response = { nutrition: [], weight: [], waist: [] };
        httpMock.expectOne(request => request.url === SUMMARY_URL).flush(response);
        httpMock.expectOne(request => request.url === environment.apiUrls.weights).flush([]);
        httpMock.expectOne(request => request.url === environment.apiUrls.waists).flush({}, { status: 500, statusText: 'Error' });
        await expect(summary).resolves.toEqual(response);
        await failed;
    });
});

describe('StatisticsService without reference measurements', () => {
    it('keeps nutrition data and reports no reference measurements for a period without body entries', async () => {
        const dateFrom = '2026-05-01T00:00:00.000Z';
        const dateTo = '2026-05-07T23:59:59.999Z';
        const summary = firstValueFrom(service.getSummary({ dateFrom, dateTo }));
        const measurements = firstValueFrom(service.getBodyMeasurements({ dateFrom, dateTo }));
        const response = { nutrition: RESPONSE, weight: [], waist: [] };
        const charts = httpMock.expectOne(request => request.url === SUMMARY_URL && request.method === 'GET');
        expect(charts.request.params.get('dateFrom')).toBe(dateFrom);
        expect(charts.request.params.get('dateTo')).toBe(dateTo);
        charts.flush(response);
        const weight = httpMock.expectOne(request => request.url === environment.apiUrls.weights && request.method === 'GET');
        const waist = httpMock.expectOne(request => request.url === environment.apiUrls.waists && request.method === 'GET');

        for (const request of [weight, waist]) {
            expect(request.request.params.get('dateFrom')).toBe(dateFrom);
            expect(request.request.params.get('dateTo')).toBe(dateTo);
            request.flush([]);
        }

        await expect(summary).resolves.toEqual(response);
        await expect(measurements).resolves.toEqual({
            weightMeasurements: { first: null, last: null, count: 0 },
            waistMeasurements: { first: null, last: null, count: 0 },
        });
    });
});
