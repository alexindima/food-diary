import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { BmiViewModel } from '../../lib/weight-history.types';
import { WeightHistoryBmiCardComponent } from './weight-history-bmi-card.component';

const BMI_VALUE = 22.9;

describe('WeightHistoryBmiCardComponent', () => {
    it('renders empty state when BMI view model is missing', () => {
        const fixture = setupComponent(null);

        expect(getText(fixture)).toContain('WEIGHT_HISTORY.BMI_NO_DATA');
    });

    it('renders BMI value and status from view model', () => {
        const fixture = setupComponent(createBmiViewModel());
        const text = getText(fixture);

        expect(text).toContain(String(BMI_VALUE));
        expect(text).toContain('WEIGHT_HISTORY.BMI_STATUS.NORMAL');
        expect(text).toContain('WEIGHT_HISTORY.BMI_DESCRIPTION.NORMAL');
    });
});

function setupComponent(viewModel: BmiViewModel | null): ComponentFixture<WeightHistoryBmiCardComponent> {
    TestBed.configureTestingModule({
        imports: [WeightHistoryBmiCardComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(WeightHistoryBmiCardComponent);
    fixture.componentRef.setInput('viewModel', viewModel);
    fixture.detectChanges();
    return fixture;
}

function getText(fixture: ComponentFixture<WeightHistoryBmiCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}

function createBmiViewModel(): BmiViewModel {
    return {
        value: BMI_VALUE,
        pointerPosition: '57.25%',
        status: {
            labelKey: 'WEIGHT_HISTORY.BMI_STATUS.NORMAL',
            descriptionKey: 'WEIGHT_HISTORY.BMI_DESCRIPTION.NORMAL',
            class: 'fd-ui-pill--success',
        },
        segments: [
            {
                labelKey: 'WEIGHT_HISTORY.BMI_STATUS.NORMAL',
                from: 18.5,
                to: 25,
                class: 'weight-history-page__bmi-segment--normal',
                width: '16.25%',
            },
        ],
    };
}
