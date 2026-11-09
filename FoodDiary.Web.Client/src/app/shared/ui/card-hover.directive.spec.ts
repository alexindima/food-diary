import { Component } from '@angular/core';
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

function getButton(fixture: ComponentFixture<CardHoverHostComponent>): HTMLButtonElement {
    const host = fixture.nativeElement as HTMLElement;
    return host.querySelector('button') as HTMLButtonElement;
}
