import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { DailyAdvice } from '../../models/daily-advice.data';
import { DailyAdviceCardComponent } from './daily-advice-card.component';

describe('DailyAdviceCardComponent', () => {
    it('maps advice tags to translation keys', async () => {
        const { component, fixture } = await setupComponentAsync({
            id: 'advice-1',
            locale: 'en',
            value: 'Eat more vegetables',
            tag: 'nutrition',
            weight: 1,
        });

        fixture.detectChanges();

        expect(component.adviceState()).toEqual({
            value: 'Eat more vegetables',
            tagKey: 'DASHBOARD.ADVICE_TAGS.NUTRITION',
        });
    });

    it('returns empty state when advice is missing and skips empty tag', async () => {
        const { component, fixture } = await setupComponentAsync(null);

        fixture.detectChanges();

        expect(component.adviceState()).toBeNull();

        fixture.componentRef.setInput('advice', createAdvice({ value: 'No tag', tag: '' }));
        fixture.detectChanges();

        expect(component.adviceState()).toEqual({ value: 'No tag', tagKey: null });
    });
});

async function setupComponentAsync(advice: DailyAdvice | null): Promise<{
    component: DailyAdviceCardComponent;
    fixture: ComponentFixture<DailyAdviceCardComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [DailyAdviceCardComponent, TranslateModule.forRoot()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(DailyAdviceCardComponent);
    fixture.componentRef.setInput('advice', advice);
    fixture.componentRef.setInput('isLoading', false);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createAdvice(overrides: Partial<DailyAdvice> = {}): DailyAdvice {
    return {
        id: 'advice-1',
        locale: 'en',
        value: 'Eat more vegetables',
        tag: 'nutrition',
        weight: 1,
        ...overrides,
    };
}
