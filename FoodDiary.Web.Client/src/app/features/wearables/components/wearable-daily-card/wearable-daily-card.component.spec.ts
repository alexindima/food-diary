import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { WearableDailySummary } from '../../models/wearable.data';
import { WearableDailyCardComponent } from './wearable-daily-card.component';

describe('WearableDailyCardComponent', () => {
    it('renders empty state when summary is missing', () => {
        const { fixture } = setupComponent(null);

        expect(getText(fixture)).toContain('WEARABLES.NO_DATA');
    });

    it('builds and renders daily metrics', () => {
        const { component, fixture } = setupComponent({
            date: '2026-05-15',
            steps: 12_345,
            heartRate: 62,
            caloriesBurned: 450,
            activeMinutes: 54,
            sleepMinutes: 455,
        });
        const text = getText(fixture);

        expect(component.metrics().map(metric => metric.key)).toEqual([
            'STEPS',
            'HEART_RATE',
            'CALORIES_BURNED',
            'ACTIVE_MINUTES',
            'SLEEP',
        ]);
        expect(text).toContain('12,345');
        expect(text).toContain('7.6');
        expect(text).toContain('WEARABLES.SLEEP');
    });
});

function setupComponent(summary: WearableDailySummary | null): {
    component: WearableDailyCardComponent;
    fixture: ComponentFixture<WearableDailyCardComponent>;
} {
    TestBed.configureTestingModule({
        imports: [WearableDailyCardComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(WearableDailyCardComponent);
    fixture.componentRef.setInput('summary', summary);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<WearableDailyCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
