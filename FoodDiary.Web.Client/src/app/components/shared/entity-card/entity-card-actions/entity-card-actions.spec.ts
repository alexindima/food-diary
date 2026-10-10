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
        const event = new MouseEvent('click', { bubbles: true });
        const stopPropagationSpy = vi.spyOn(event, 'stopPropagation');
        const actionSpy = vi.fn();
        component['action'].subscribe(actionSpy);
        fixture.detectChanges();

        (fixture.nativeElement as HTMLElement).querySelector('button')?.dispatchEvent(event);

        expect(stopPropagationSpy).toHaveBeenCalledOnce();
        expect(actionSpy).toHaveBeenCalledOnce();
    });

    it('stops keyboard events from reaching the parent card', async () => {
        const fixture = await setupEntityCardActionsAsync();
        const event = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true });
        const stopPropagationSpy = vi.spyOn(event, 'stopPropagation');
        fixture.detectChanges();

        (fixture.nativeElement as HTMLElement).querySelector('button')?.dispatchEvent(event);

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
