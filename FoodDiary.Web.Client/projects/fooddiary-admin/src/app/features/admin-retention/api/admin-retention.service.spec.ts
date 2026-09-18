import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { expect, it } from 'vitest';

import { AdminRetentionService } from './admin-retention.service';

it('omits absent dates rather than sending undefined for all-time cohorts', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    TestBed.inject(AdminRetentionService)
        .getReport({ from: '2026-09-01', to: '2026-09-18', cohortFrom: '1970-01-01', cohortTo: undefined })
        .subscribe();
    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne(req => req.url.endsWith('/admin/analytics/retention'));
    expect(request.request.params.get('cohortFrom')).toBe('1970-01-01');
    expect(request.request.params.has('cohortTo')).toBe(false);
    expect(request.request.params.get('from')).toBe('2026-09-01');
    request.flush({});
    http.verify();
});
