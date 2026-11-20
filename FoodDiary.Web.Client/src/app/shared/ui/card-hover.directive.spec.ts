import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { FdCardHoverDirective } from './card-hover.directive';

const hoverShadow = '0 8px 24px rgba(0, 0, 0, 0.16)';

@Component({
    imports: [FdCardHoverDirective],
    template:
        ' <button fdCardHover [fdCardHoverDisabled]="disabled" [fdCardHoverTransform]="transform" [fdCardHoverShadow]="shadow">Hover</button> ',
})
class CardHoverHostComponent {
    public disabled = false;
    public transform: string | null = 'translateY(-2px)';
    public shadow: string | null = hoverShadow;
}

describe('FdCardHoverDirective', () => {
    it('should apply and clear hover styles', () => {
        const fixture = createFixture();
        const element = getButton(fixture);

        element.dispatchEvent(new Event('mouseenter'));
        fixture.detectChanges();

        expect(element.style.transform).toBe('translateY(-2px)');
        expectHoverShadow(element);

        element.dispatchEvent(new Event('mouseleave'));
        fixture.detectChanges();

        expect(element.style.transform).toBe('');
        expect(element.style.boxShadow).toBe('');
    });

    it('should apply focus styles for keyboard users', () => {
        const fixture = createFixture();
        const element = getButton(fixture);

        element.dispatchEvent(new Event('focusin'));
        fixture.detectChanges();

        expect(element.style.transform).toBe('translateY(-2px)');
        expectHoverShadow(element);
    });

    it('should remove hover behavior when disabled', () => {
        const fixture = createFixture();
        const element = getButton(fixture);
        fixture.componentInstance.disabled = true;
        fixture.detectChanges();

        element.dispatchEvent(new Event('mouseenter'));
        fixture.detectChanges();

        expect(element.style.cursor).toBe('');
        expect(element.style.transition).toBe('');
        expect(element.style.transform).toBe('');
        expect(element.style.boxShadow).toBe('');
    });
});

const ORIGINAL_CARD_STYLES = 'transform: scale(0.98); box-shadow: 0 1px 3px #222; cursor: grab; transition: opacity 0.3s ease;';

@Component({
    imports: [FdCardHoverDirective],
    template: `<button type="button" fdCardHover [fdCardHoverDisabled]="disabled()" style="${ORIGINAL_CARD_STYLES}">Open card</button>`,
})
class InlineStyleCardHostComponent {
    public readonly disabled = signal(false);
}

describe('FdCardHoverDirective with existing inline styles', () => {
    it('uses the theme defaults for keyboard focus and restores the original styles on blur', () => {
        const fixture = TestBed.createComponent(InlineStyleCardHostComponent);
        fixture.detectChanges();
        const card = getButton(fixture);
        const original = originalCardStyle();

        card.focus();

        expect(document.activeElement).toBe(card);
        expect(card.style.transform).toBe('var(--fd-card-hover-transform)');
        expect(card.style.boxShadow).toBe('var(--fd-card-hover-shadow)');
        card.blur();

        expect(card.style.transform).toBe(original.transform);
        expect(card.style.boxShadow).toBe(original.boxShadow);
        expect(card.style.cursor).toBe('pointer');
    });

    it('restores all inline styles when disabled and when destroyed while still hovered', () => {
        const fixture = TestBed.createComponent(InlineStyleCardHostComponent);
        fixture.detectChanges();
        const card = getButton(fixture);
        const original = originalCardStyle();
        card.dispatchEvent(new MouseEvent('mouseenter'));
        expect(card.style.transform).toBe('var(--fd-card-hover-transform)');

        fixture.componentInstance.disabled.set(true);
        fixture.detectChanges();
        card.dispatchEvent(new FocusEvent('focusin'));

        expectCardStyle(card, original);
        fixture.componentInstance.disabled.set(false);
        fixture.detectChanges();
        card.dispatchEvent(new MouseEvent('mouseenter'));
        expect(card.style.transform).toBe('var(--fd-card-hover-transform)');

        fixture.destroy();

        expectCardStyle(card, original);
    });
});

function originalCardStyle(): CSSStyleDeclaration {
    const sample = document.createElement('button');
    sample.style.cssText = ORIGINAL_CARD_STYLES;
    return sample.style;
}

function expectCardStyle(card: HTMLButtonElement, original: CSSStyleDeclaration): void {
    expect(card.style.transform).toBe(original.transform);
    expect(card.style.boxShadow).toBe(original.boxShadow);
    expect(card.style.cursor).toBe(original.cursor);
    expect(card.style.transition).toBe(original.transition);
}

function expectHoverShadow(element: HTMLButtonElement): void {
    // Compare the shadow in the DOM's own serialization format.
    const expectedStyle = document.createElement('button').style;
    expectedStyle.boxShadow = hoverShadow;
    expect(element.style.boxShadow).not.toBe('');
    expect(element.style.boxShadow).toBe(expectedStyle.boxShadow);
}

function createFixture(): ComponentFixture<CardHoverHostComponent> {
    const fixture = TestBed.createComponent(CardHoverHostComponent);
    fixture.detectChanges();
    return fixture;
}

function getButton<T>(fixture: ComponentFixture<T>): HTMLButtonElement {
    const host = fixture.nativeElement as HTMLElement;
    return host.querySelector('button') as HTMLButtonElement;
}
