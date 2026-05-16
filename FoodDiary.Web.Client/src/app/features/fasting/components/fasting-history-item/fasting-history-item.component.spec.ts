import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { FastingHistorySessionViewModel } from '../../pages/fasting-page-lib/fasting-page.types';
import { FastingHistoryItemComponent } from './fasting-history-item.component';

describe('FastingHistoryItemComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [FastingHistoryItemComponent, TranslateModule.forRoot()],
        });
    });

    it('renders session summary and notes', () => {
        const fixture = createComponent(createHistoryItem());
        const text = getElement(fixture).textContent;

        expect(text).toContain('May 16');
        expect(text).toContain('Intermittent');
        expect(text).toContain('16:8');
        expect(text).toContain('Felt good');
    });

    it('emits chart and toggle actions when check-ins are available', () => {
        const fixture = createComponent(createHistoryItem({ canViewChart: true }));
        const chartOpen = vi.fn();
        const historyToggle = vi.fn();
        fixture.componentInstance.chartOpen.subscribe(chartOpen);
        fixture.componentInstance.historyToggle.subscribe(historyToggle);
        const element = getElement(fixture);

        getButtonByText(element, 'FASTING.SHOW_CHECK_IN_CHART').click();
        getButtonByText(element, 'FASTING.SHOW_CHECK_INS').click();

        expect(chartOpen).toHaveBeenCalledWith(fixture.componentInstance.historyItem().session);
        expect(historyToggle).toHaveBeenCalledWith('session-1');
    });

    it('does not render check-in controls when the session has no check-ins', () => {
        const fixture = createComponent(createHistoryItem({ hasCheckIns: false, checkInCount: 0, canViewChart: false }));
        const text = getElement(fixture).textContent;

        expect(text).not.toContain('FASTING.HISTORY_CHECK_INS_COUNT');
        expect(text).not.toContain('FASTING.SHOW_CHECK_IN_CHART');
    });
});

function createComponent(historyItem: FastingHistorySessionViewModel): ComponentFixture<FastingHistoryItemComponent> {
    const fixture = TestBed.createComponent(FastingHistoryItemComponent);
    fixture.componentRef.setInput('historyItem', historyItem);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<FastingHistoryItemComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function getButtonByText(element: HTMLElement, text: string): HTMLElement {
    const button = Array.from(element.querySelectorAll<HTMLElement>('fd-ui-button')).find(item => item.textContent.includes(text));
    if (button === undefined) {
        throw new Error(`Button with text "${text}" was not found.`);
    }

    return button;
}

function createHistoryItem(overrides: Partial<FastingHistorySessionViewModel> = {}): FastingHistorySessionViewModel {
    return {
        session: {
            id: 'session-1',
            startedAtUtc: '2026-05-16T08:00:00.000Z',
            endedAtUtc: '2026-05-16T18:00:00.000Z',
            initialPlannedDurationHours: 16,
            addedDurationHours: 0,
            plannedDurationHours: 16,
            protocol: 'F16_8',
            planType: 'Intermittent',
            occurrenceKind: 'FastingWindow',
            cyclicFastDays: null,
            cyclicEatDays: null,
            cyclicEatDayFastHours: null,
            cyclicEatDayEatingWindowHours: null,
            cyclicPhaseDayNumber: null,
            cyclicPhaseDayTotal: null,
            isCompleted: true,
            status: 'Completed',
            notes: 'Felt good',
            checkInAtUtc: null,
            hungerLevel: null,
            energyLevel: null,
            moodLevel: null,
            symptoms: [],
            checkInNotes: null,
            checkIns: [],
        },
        startedAtLabel: 'May 16',
        accentColor: 'green',
        sessionTypeLabel: 'Intermittent',
        protocolDisplay: '16:8',
        badgeKey: 'FASTING.STATUS.COMPLETED',
        hasCheckIns: true,
        checkInCount: 2,
        canViewChart: false,
        isExpanded: false,
        checkInRegionId: 'check-ins-session-1',
        toggleKey: 'FASTING.SHOW_CHECK_INS',
        visibleCheckIns: [],
        canLoadMoreCheckIns: false,
        ...overrides,
    };
}
