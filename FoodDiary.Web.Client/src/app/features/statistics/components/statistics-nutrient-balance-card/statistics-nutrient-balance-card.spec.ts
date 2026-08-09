import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { StatisticsNutrientBalanceCardComponent } from './statistics-nutrient-balance-card';

describe('StatisticsNutrientBalanceCardComponent', () => {
    it('renders accessible completion rows and clamps progress', async () => {
        await TestBed.configureTestingModule({
            imports: [StatisticsNutrientBalanceCardComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        const fixture = TestBed.createComponent(StatisticsNutrientBalanceCardComponent);
        fixture.componentRef.setInput('items', [
            { key: 'protein', current: 109, goal: 140 },
            { key: 'fat', current: 90, goal: 80 },
        ]);
        fixture.detectChanges();
        const progressBars = (fixture.nativeElement as HTMLElement).querySelectorAll('[role="progressbar"]');

        expect(progressBars).toHaveLength(2);
        expect(progressBars[0].getAttribute('aria-valuenow')).toBe('78');
        expect(progressBars[1].getAttribute('aria-valuenow')).toBe('100');
    });
});
