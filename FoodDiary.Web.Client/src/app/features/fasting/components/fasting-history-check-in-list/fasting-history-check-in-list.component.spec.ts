import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { FastingHistorySessionViewModel } from '../../pages/fasting-page-lib/fasting-page.types';
import { FastingHistoryCheckInListComponent } from './fasting-history-check-in-list.component';

describe('FastingHistoryCheckInListComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [FastingHistoryCheckInListComponent, TranslateModule.forRoot()],
        });
    });

    it('renders nothing while collapsed', () => {
        const fixture = createComponent(createHistoryItem({ isExpanded: false }));

        expect(getElement(fixture).textContent.trim()).toBe('');
    });

    it('renders visible check-ins and emits load-more requests', () => {
        const fixture = createComponent(createHistoryItem({ canLoadMoreCheckIns: true }));
        const checkInsLoadMore = vi.fn();
        fixture.componentInstance.checkInsLoadMore.subscribe(checkInsLoadMore);
        const element = getElement(fixture);

        expect(element.textContent).toContain('First check-in');
        getButtonByText(element, 'FASTING.LOAD_MORE_CHECK_INS').click();

        expect(checkInsLoadMore).toHaveBeenCalledWith('session-1');
    });
});

function createComponent(historyItem: FastingHistorySessionViewModel): ComponentFixture<FastingHistoryCheckInListComponent> {
    const fixture = TestBed.createComponent(FastingHistoryCheckInListComponent);
    fixture.componentRef.setInput('historyItem', historyItem);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<FastingHistoryCheckInListComponent>): HTMLElement {
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
            notes: null,
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
        checkInCount: 1,
        canViewChart: false,
        isExpanded: true,
        checkInRegionId: 'check-ins-session-1',
        toggleKey: 'FASTING.HIDE_CHECK_INS',
        visibleCheckIns: [
            {
                checkIn: {
                    id: 'check-in-1',
                    checkedInAtUtc: '2026-05-16T10:00:00.000Z',
                    hungerLevel: 3,
                    energyLevel: 3,
                    moodLevel: 4,
                    symptoms: [],
                    notes: null,
                },
                checkedInAtLabel: '10:00',
                relativeCheckedInAt: null,
                summary: 'First check-in',
                symptomLabels: [],
            },
        ],
        canLoadMoreCheckIns: false,
        ...overrides,
    };
}
