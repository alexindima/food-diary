import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiActivateDirective } from '../interaction/fd-ui-activate.directive';
import { FdUiCardComponent } from './fd-ui-card';
import { FdUiCardActionsDirective } from './fd-ui-card-actions.directive';

@Component({
    imports: [FdUiCardComponent],
    template: '<fd-ui-card><p class="projected">Projected content</p></fd-ui-card>',
})
class CardWithContentHostComponent {}

@Component({
    imports: [FdUiCardComponent, FdUiCardActionsDirective],
    template: `
        <fd-ui-card>
            @if (canEdit()) {
                <button fdUiCardActions type="button" (click)="canEdit.set(false)">Finish editing</button>
            }
            <p>Daily progress</p>
        </fd-ui-card>
    `,
})
class CardWithActionsHostComponent {
    public readonly canEdit = signal(true);
}

describe('FdUiCardComponent', () => {
    let component: FdUiCardComponent;
    let fixture: ComponentFixture<FdUiCardComponent>;

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const requireElement = (selector: string): HTMLElement => {
        const element = host().querySelector<HTMLElement>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiCardComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiCardComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have default card class', () => {
        const cardClass = component['cardClass']();
        expect(cardClass).toContain('fd-ui-card');
        expect(cardClass).toContain('fd-ui-card--appearance-default');
        expect(cardClass).not.toContain('fd-ui-card--subtle');
    });

    it('should add subtle class when subtle is true', () => {
        fixture.componentRef.setInput('subtle', true);
        fixture.detectChanges();

        const cardClass = component['cardClass']();
        expect(cardClass).toContain('fd-ui-card--subtle');
    });

    it('should set appearance class', () => {
        fixture.componentRef.setInput('appearance', 'product');
        fixture.detectChanges();

        const cardClass = component['cardClass']();
        expect(cardClass).toContain('fd-ui-card--appearance-product');
        expect(cardClass).not.toContain('fd-ui-card--appearance-default');
    });

    it('should display title', () => {
        fixture.componentRef.setInput('title', 'Test Title');
        fixture.detectChanges();

        const titleEl = requireElement('.fd-ui-card__title');
        expect(titleEl.textContent.trim()).toBe('Test Title');
    });

    it('should display meta', () => {
        fixture.componentRef.setInput('meta', '100 kcal');
        fixture.detectChanges();

        const metaEl = requireElement('.fd-ui-card__meta');
        expect(metaEl.textContent.trim()).toBe('100 kcal');
    });

    it('should project content', () => {
        const hostFixture = TestBed.createComponent(CardWithContentHostComponent);
        hostFixture.detectChanges();

        const projectedHost = hostFixture.nativeElement as HTMLElement;
        const projected = projectedHost.querySelector<HTMLElement>('.projected');
        expect(projected?.textContent.trim()).toBe('Projected content');
    });
});

describe('FdUiCardComponent with host activation', () => {
    it('keeps a projected action independent of the keyboard-activated card', async () => {
        await TestBed.configureTestingModule({ imports: [CardWithActionsHostComponent] })
            .overrideComponent(FdUiCardComponent, { add: { hostDirectives: [FdUiActivateDirective] } })
            .compileComponents();
        const fixture = TestBed.createComponent(CardWithActionsHostComponent);
        fixture.detectChanges();

        const card = fixture.debugElement.query(By.directive(FdUiCardComponent));
        const element = card.nativeElement as HTMLElement;
        const activated = vi.fn();
        card.injector.get(FdUiActivateDirective).fdUiActivated.subscribe(activated);
        const headerAction = element.querySelector<HTMLButtonElement>('.fd-ui-card__actions button');
        expect(element.getAttribute('role')).toBe('button');
        expect(headerAction?.textContent.trim()).toBe('Finish editing');
        headerAction?.click();
        fixture.detectChanges();
        expect(element.querySelector('header')).toBeNull();
        expect(element.querySelector('.fd-ui-card__content p')?.textContent).toBe('Daily progress');
        expect(activated).not.toHaveBeenCalled();

        element.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        expect(activated).toHaveBeenCalledOnce();
    });
});
