import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { WeeklyCheckInTrendCardViewModel } from '../../lib/weekly-check-in.types';
import { WeeklyCheckInTrendsComponent } from './weekly-check-in-trends.component';

describe('WeeklyCheckInTrendsComponent', () => {
    it('renders nothing when trends list is empty', () => {
        const fixture = setupComponent([]);

        expect((fixture.nativeElement as HTMLElement).querySelector('.check-in__trend')).toBeNull();
    });

    it('renders trend cards', () => {
        const fixture = setupComponent([
            {
                key: 'calories',
                labelKey: 'WEEKLY_CHECK_IN.CALORIES',
                value: 120,
                unitKey: 'GENERAL.UNITS.KCAL',
                unitSeparator: ' ',
                numberFormat: '1.0-0',
                valuePrefix: '+',
                color: 'var(--fd-color-green-500)',
                icon: 'trending_up',
            },
        ]);
        const text = getText(fixture);

        expect(text).toContain('WEEKLY_CHECK_IN.CALORIES');
        expect(text).toContain('+120 GENERAL.UNITS.KCAL');
    });
});

function setupComponent(trends: WeeklyCheckInTrendCardViewModel[]): ComponentFixture<WeeklyCheckInTrendsComponent> {
    TestBed.configureTestingModule({
        imports: [WeeklyCheckInTrendsComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(WeeklyCheckInTrendsComponent);
    fixture.componentRef.setInput('trends', trends);
    fixture.detectChanges();
    return fixture;
}

function getText(fixture: ComponentFixture<WeeklyCheckInTrendsComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
