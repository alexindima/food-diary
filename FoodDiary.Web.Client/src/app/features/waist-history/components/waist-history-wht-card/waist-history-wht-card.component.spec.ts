import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { WhtViewModel } from '../../lib/waist-history.types';
import { WaistHistoryWhtCardComponent } from './waist-history-wht-card.component';

const WHT_VALUE = 0.46;

describe('WaistHistoryWhtCardComponent', () => {
    it('renders empty state when WHT view model is missing', () => {
        const fixture = setupComponent(null);

        expect(getText(fixture)).toContain('WAIST_HISTORY.WHT_NO_DATA');
    });

    it('renders WHT value and status from view model', () => {
        const fixture = setupComponent(createWhtViewModel());
        const text = getText(fixture);

        expect(text).toContain(String(WHT_VALUE));
        expect(text).toContain('WAIST_HISTORY.WHT_STATUS.NORMAL');
        expect(text).toContain('WAIST_HISTORY.WHT_STATUS_DESC.NORMAL');
    });
});

function setupComponent(viewModel: WhtViewModel | null): ComponentFixture<WaistHistoryWhtCardComponent> {
    TestBed.configureTestingModule({
        imports: [WaistHistoryWhtCardComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(WaistHistoryWhtCardComponent);
    fixture.componentRef.setInput('viewModel', viewModel);
    fixture.detectChanges();
    return fixture;
}

function getText(fixture: ComponentFixture<WaistHistoryWhtCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}

function createWhtViewModel(): WhtViewModel {
    return {
        value: WHT_VALUE,
        pointerPosition: '57.5%',
        status: {
            labelKey: 'WAIST_HISTORY.WHT_STATUS.NORMAL',
            descriptionKey: 'WAIST_HISTORY.WHT_STATUS_DESC.NORMAL',
            class: 'waist-history-page__wht-status--normal',
        },
        segments: [
            {
                labelKey: 'WAIST_HISTORY.WHT_SEGMENTS.NORMAL',
                from: 0.4,
                to: 0.5,
                class: 'waist-history-page__wht-segment--normal',
                width: '12.5%',
            },
        ],
    };
}
