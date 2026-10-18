import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { FeaturesComponent } from './features';
import { FEATURE_CATEGORIES } from './features.config';

let fixture: ComponentFixture<FeaturesComponent>;

beforeEach(async () => {
    await TestBed.configureTestingModule({
        imports: [FeaturesComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(FeaturesComponent);
    fixture.detectChanges();
});

function tabs(): HTMLButtonElement[] {
    return [...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('[role="tab"]')];
}

function panel(): HTMLElement {
    const element = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('[role="tabpanel"]');
    if (element === null) {
        throw new Error('Expected the active category panel.');
    }
    return element;
}

describe('Public feature category accessibility', () => {
    it('has one tab stop and a labelled, keyboard-reachable content panel', () => {
        expect(tabs().filter(tab => tab.tabIndex === 0)).toEqual([tabs()[0]]);
        expect(panel().tabIndex).toBe(0);
        expect(panel().getAttribute('aria-labelledby')).toBe(tabs()[0].id);
    });

    it('retains pointer selection and updates the associated content', () => {
        tabs()[2].click();
        fixture.detectChanges();

        expect(tabs()[2].getAttribute('aria-selected')).toBe('true');
        expect(tabs().filter(tab => tab.tabIndex === 0)).toEqual([tabs()[2]]);
        expect(panel().id).toBe(FEATURE_CATEGORIES[2].panelId);
        expect(panel().textContent).toContain(FEATURE_CATEGORIES[2].itemKeys[0].titleKey);
    });
});

describe('Public feature category keyboard navigation', () => {
    it.each([
        { key: 'ArrowRight', from: 0, to: 1 },
        { key: 'ArrowLeft', from: 1, to: 0 },
        { key: 'ArrowRight', from: 4, to: 0 },
        { key: 'ArrowLeft', from: 0, to: 4 },
        { key: 'Home', from: 3, to: 0 },
        { key: 'End', from: 1, to: 4 },
    ])('moves focus and the active panel for $key from $from to $to', ({ key, from, to }) => {
        tabs()[from].click();
        fixture.detectChanges();
        tabs()[from].focus();
        const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });

        tabs()[from].dispatchEvent(event);
        fixture.detectChanges();

        expect(event.defaultPrevented).toBe(true);
        expect(document.activeElement).toBe(tabs()[to]);
        expect(tabs().filter(tab => tab.getAttribute('aria-selected') === 'true')).toEqual([tabs()[to]]);
        expect(tabs().filter(tab => tab.tabIndex === 0)).toEqual([tabs()[to]]);
        expect(panel().getAttribute('aria-labelledby')).toBe(tabs()[to].id);
        expect(panel().textContent).toContain(FEATURE_CATEGORIES[to].itemKeys[0].titleKey);
    });

    it.each(['ArrowDown', 'ArrowUp', 'Tab'])('retains browser behavior for %s in the horizontal tablist', key => {
        tabs()[0].focus();
        const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });

        tabs()[0].dispatchEvent(event);
        fixture.detectChanges();

        expect(event.defaultPrevented).toBe(false);
        expect(document.activeElement).toBe(tabs()[0]);
        expect(tabs()[0].getAttribute('aria-selected')).toBe('true');
    });
});
