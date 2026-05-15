import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiCardComponent } from './fd-ui-card.component';

@Component({
    standalone: true,
    imports: [FdUiCardComponent],
    template: '<fd-ui-card><p class="projected">Projected content</p></fd-ui-card>',
})
class CardWithContentHostComponent {}

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
        const cardClass = component.cardClass();
        expect(cardClass).toContain('fd-ui-card');
        expect(cardClass).toContain('fd-ui-card--appearance-default');
        expect(cardClass).not.toContain('fd-ui-card--subtle');
    });

    it('should add subtle class when subtle is true', () => {
        fixture.componentRef.setInput('subtle', true);
        fixture.detectChanges();

        const cardClass = component.cardClass();
        expect(cardClass).toContain('fd-ui-card--subtle');
    });

    it('should set appearance class', () => {
        fixture.componentRef.setInput('appearance', 'product');
        fixture.detectChanges();

        const cardClass = component.cardClass();
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
