import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import type { FastingMessageViewModel } from '../../pages/fasting-page-lib/fasting-page.types';
import { FastingInsightsSectionComponent } from './fasting-insights-section.component';

describe('FastingInsightsSectionComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [FastingInsightsSectionComponent, TranslateModule.forRoot()],
        });
    });

    it('renders nothing without insights', () => {
        const fixture = createComponent([]);

        expect(getElement(fixture).textContent.trim()).toBe('');
    });

    it('renders insight title and body', () => {
        const fixture = createComponent([createInsight()]);
        const text = getElement(fixture).textContent;

        expect(text).toContain('FASTING.INSIGHTS.TITLE');
        expect(text).toContain('Good consistency');
        expect(text).toContain('You checked in today');
    });
});

function createComponent(insights: FastingMessageViewModel[]): ComponentFixture<FastingInsightsSectionComponent> {
    const fixture = TestBed.createComponent(FastingInsightsSectionComponent);
    fixture.componentRef.setInput('insights', insights);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<FastingInsightsSectionComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createInsight(): FastingMessageViewModel {
    return {
        message: {
            id: 'insight-1',
            titleKey: 'FASTING.INSIGHT',
            bodyKey: 'FASTING.INSIGHT_BODY',
            tone: 'positive',
            bodyParams: null,
        },
        severity: 'success',
        title: 'Good consistency',
        body: 'You checked in today',
    };
}
