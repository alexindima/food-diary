import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { CycleResponse } from '../../models/cycle.data';
import { CycleCalendarCardComponent } from './cycle-calendar-card';

const SELECTED_YEAR = 2025;
const MARCH_INDEX = 2;
const SELECTED_DAY = 15;
const CYCLE: CycleResponse = {
    id: 'cycle-1',
    userId: 'user-1',
    mode: 0,
    goal: 0,
    reproductiveState: 0,
    hideFromDashboard: false,
    confidence: 2,
    trackingStartDate: '2025-03-01T00:00:00Z',
    averageCycleLength: 28,
    averagePeriodLength: 5,
    lutealLength: 14,
    isRegular: true,
    isOnboardingComplete: true,
    showFertilityEstimates: true,
    discreetNotifications: false,
    bleedingEntries: [
        {
            id: 'bleeding-1',
            cycleProfileId: 'cycle-1',
            date: '2025-03-15T00:00:00Z',
            type: 0,
            flow: 2,
        },
    ],
    symptoms: [],
    factors: [],
    fertilitySignals: [],
    menstrualEpisodes: [
        {
            id: 'episode-1',
            cycleProfileId: 'cycle-1',
            startDate: '2025-03-14T00:00:00Z',
            endDate: '2025-03-16T00:00:00Z',
            status: 1,
            excludedFromPredictions: false,
        },
    ],
    predictions: {
        nextPeriodStartFrom: '2025-04-11T00:00:00Z',
        nextPeriodStartTo: '2025-04-13T00:00:00Z',
        confidence: 'medium',
        rationale: 'test',
    },
};

describe('CycleCalendarCardComponent', () => {
    let component: CycleCalendarCardComponent;
    let fixture: ComponentFixture<CycleCalendarCardComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [CycleCalendarCardComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();

        fixture = TestBed.createComponent(CycleCalendarCardComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('cycle', CYCLE);
        fixture.componentRef.setInput('locale', 'en');
        fixture.detectChanges();
    });

    it('maps logged, confirmed and predicted dates to calendar markers', () => {
        const markers = component['markers']();

        expect(markers).toContainEqual(expect.objectContaining({ date: '2025-03-15', tone: 'danger' }));
        expect(markers).toContainEqual(expect.objectContaining({ date: '2025-03-14', tone: 'brand' }));
        expect(markers).toContainEqual(expect.objectContaining({ date: '2025-03-16', tone: 'brand' }));
        expect(markers).toContainEqual(expect.objectContaining({ date: '2025-04-11', tone: 'warning' }));
        expect(markers).toContainEqual(expect.objectContaining({ date: '2025-04-13', tone: 'warning' }));
    });

    it('emits the selected date as a cycle date key', () => {
        const emitSpy = vi.spyOn(component.dateSelected, 'emit');

        component['selectDate'](new Date(SELECTED_YEAR, MARCH_INDEX, SELECTED_DAY));

        expect(emitSpy).toHaveBeenCalledWith('2025-03-15');
    });
});
