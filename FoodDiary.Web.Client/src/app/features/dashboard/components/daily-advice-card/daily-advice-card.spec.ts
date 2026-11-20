import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { DailyAdvice } from '../../../../shared/models/daily-advice.data';
import { DailyAdviceCardComponent } from './daily-advice-card';

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

        expect(component['adviceState']()).toEqual({
            value: 'Eat more vegetables',
            tagKey: 'DASHBOARD.ADVICE_TAGS.NUTRITION',
        });
    });

    it('returns empty state when advice is missing and skips empty tag', async () => {
        const { component, fixture } = await setupComponentAsync(null);

        fixture.detectChanges();

        expect(component['adviceState']()).toBeNull();

        fixture.componentRef.setInput('advice', createAdvice({ value: 'No tag', tag: '' }));
        fixture.detectChanges();

        expect(component['adviceState']()).toEqual({ value: 'No tag', tagKey: null });
    });

    it('hides the previous category while loading and shows new untagged advice without a category', async () => {
        const { fixture } = await setupComponentAsync(createAdvice());
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const advice = (): string | undefined => host.querySelector('.daily-advice-card__text')?.textContent.trim();
        const category = (): string | undefined => host.querySelector('.daily-advice-card__chip')?.textContent.trim();
        expect(advice()).toBe('Eat more vegetables');
        expect(category()).toBe('DASHBOARD.ADVICE_TAGS.NUTRITION');

        fixture.componentRef.setInput('isLoading', true);
        fixture.detectChanges();
        expect(host.textContent).toContain('DASHBOARD.ADVICE_LOADING');
        expect(category()).toBeUndefined();
        expect(advice()).toBeUndefined();

        fixture.componentRef.setInput('advice', createAdvice({ value: 'Take a short walk', tag: null }));
        fixture.componentRef.setInput('isLoading', false);
        fixture.detectChanges();
        expect(advice()).toBe('Take a short walk');
        expect(category()).toBeUndefined();
        expect(host.textContent).not.toContain('DASHBOARD.ADVICE_LOADING');
    });
});

async function setupComponentAsync(advice: DailyAdvice | null): Promise<{
    component: DailyAdviceCardComponent;
    fixture: ComponentFixture<DailyAdviceCardComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [DailyAdviceCardComponent],
            providers: [provideTranslateTesting()],
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
