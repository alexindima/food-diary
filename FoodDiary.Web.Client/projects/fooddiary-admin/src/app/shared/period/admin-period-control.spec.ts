import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { AdminPeriodControlComponent } from './admin-period-control';

describe('AdminPeriodControlComponent independent filters', () => {
    it.each(['', 'activity_'])('updates only its own period with prefix %s', async prefix => {
        await TestBed.configureTestingModule({
            imports: [AdminPeriodControlComponent],
            providers: [provideRouter([]), ...provideTranslateTesting()],
        }).compileComponents();
        const router = TestBed.inject(Router);
        await router.navigate([], { queryParams: { period: '90d', activity_period: '30d' } });
        const fixture = TestBed.createComponent(AdminPeriodControlComponent);
        fixture.componentRef.setInput('queryPrefix', prefix);
        fixture.detectChanges();
        fixture.componentInstance['select']('7d');
        await fixture.whenStable();
        const params = router.parseUrl(router.url).queryParams;
        expect(params[`${prefix}period`]).toBe('7d');
        expect(params[prefix === '' ? 'activity_period' : 'period']).toBe(prefix === '' ? '30d' : '90d');
    });
});
