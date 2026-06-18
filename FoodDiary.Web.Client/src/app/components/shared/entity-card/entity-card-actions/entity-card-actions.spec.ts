import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { EntityCardActionsComponent } from './entity-card-actions';

async function setupEntityCardActionsAsync(): Promise<ComponentFixture<EntityCardActionsComponent>> {
    await TestBed.configureTestingModule({
        imports: [EntityCardActionsComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(EntityCardActionsComponent);
    fixture.componentRef.setInput('actionIcon', 'add');
    return fixture;
}

describe('EntityCardActionsComponent', () => {
    it('stops propagation and emits action', async () => {
        const fixture = await setupEntityCardActionsAsync();
        const component = fixture.componentInstance;
        const stopPropagationSpy = vi.fn();
        const event = { stopPropagation: stopPropagationSpy } as unknown as Event;
        const actionSpy = vi.fn();
        component['action'].subscribe(actionSpy);
        fixture.detectChanges();

        component['emitCardAction'](event);

        expect(stopPropagationSpy).toHaveBeenCalledOnce();
        expect(actionSpy).toHaveBeenCalledOnce();
    });

    it('stops keyboard events from reaching the parent card', async () => {
        const fixture = await setupEntityCardActionsAsync();
        const component = fixture.componentInstance;
        const stopPropagationSpy = vi.fn();
        fixture.detectChanges();

        component['stopCardKeyboardEvent']({ stopPropagation: stopPropagationSpy } as unknown as KeyboardEvent);

        expect(stopPropagationSpy).toHaveBeenCalledOnce();
    });

    it('does not render an empty aria label when action label is missing', async () => {
        const fixture = await setupEntityCardActionsAsync();
        fixture.detectChanges();

        const el = fixture.nativeElement as HTMLElement;
        const button = el.querySelector('.entity-card__action-button');

        expect(button?.hasAttribute('aria-label')).toBe(false);
    });

    it('renders action aria label when provided', async () => {
        const fixture = await setupEntityCardActionsAsync();
        fixture.componentRef.setInput('actionAriaLabel', 'Add item');
        fixture.detectChanges();

        const el = fixture.nativeElement as HTMLElement;
        const button = el.querySelector('.entity-card__action-button');

        expect(button?.getAttribute('aria-label')).toBe('Add item');
    });
});
