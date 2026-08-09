import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { StatisticsBodyTrendCardComponent } from './statistics-body-trend-card';

describe('StatisticsBodyTrendCardComponent', () => {
    it('renders the current weight, change, chart, and history action', async () => {
        await TestBed.configureTestingModule({
            imports: [StatisticsBodyTrendCardComponent],
            providers: [provideTranslateTesting(), provideRouter([])],
        }).compileComponents();
        const fixture = TestBed.createComponent(StatisticsBodyTrendCardComponent);
        fixture.componentRef.setInput('data', {
            currentWeight: 113,
            change: -3,
            timeframeDays: 30,
            points: [
                { label: '20 Jul', value: 116 },
                { label: '4 Aug', value: 113 },
            ],
        });
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;

        expect(root.querySelector('fd-ui-line-chart')).not.toBeNull();
        expect(root.querySelector('fd-ui-button')).not.toBeNull();
        expect(root.textContent).toContain('113');
        expect(root.textContent).toContain('-3');
    });
});
