import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { FastingHistorySessionViewModel } from '../../pages/fasting-page-lib/fasting-page.types';
import { FastingHistoryCardComponent } from './fasting-history-card.component';

describe('FastingHistoryCardComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [FastingHistoryCardComponent, TranslateModule.forRoot()],
        });
    });

    it('renders empty state without history items', () => {
        const fixture = createComponent([]);

        expect(getElement(fixture).textContent).toContain('FASTING.NO_HISTORY');
    });

    it('renders history items and emits load-more requests', () => {
        const fixture = createComponent([createHistoryItem()], true);
        const historyLoadMore = vi.fn();
        fixture.componentInstance.historyLoadMore.subscribe(historyLoadMore);
        const element = getElement(fixture);

        expect(element.textContent).toContain('May 16');
        getButtonByText(element, 'FASTING.LOAD_MORE_HISTORY').click();

        expect(historyLoadMore).toHaveBeenCalledTimes(1);
    });

    it('hides load-more action when there is no next history page', () => {
        const fixture = createComponent([createHistoryItem()], false);

        expect(getElement(fixture).textContent).not.toContain('FASTING.LOAD_MORE_HISTORY');
    });
});

function createComponent(
    historyItems: FastingHistorySessionViewModel[],
    canLoadMoreHistory = false,
): ComponentFixture<FastingHistoryCardComponent> {
    const fixture = TestBed.createComponent(FastingHistoryCardComponent);
    fixture.componentRef.setInput('historyItems', historyItems);
    fixture.componentRef.setInput('canLoadMoreHistory', canLoadMoreHistory);
    fixture.componentRef.setInput('isLoadingMoreHistory', false);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<FastingHistoryCardComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function getButtonByText(element: HTMLElement, text: string): HTMLElement {
    const button = Array.from(element.querySelectorAll<HTMLElement>('fd-ui-button')).find(item => item.textContent.includes(text));
    if (button === undefined) {
        throw new Error(`Button with text "${text}" was not found.`);
    }

    return button;
}

function createHistoryItem(): FastingHistorySessionViewModel {
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
        hasCheckIns: false,
        checkInCount: 0,
        canViewChart: false,
        isExpanded: false,
        checkInRegionId: 'check-ins-session-1',
        toggleKey: 'FASTING.SHOW_CHECK_INS',
        visibleCheckIns: [],
        canLoadMoreCheckIns: false,
    };
}
