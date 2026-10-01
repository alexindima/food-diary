import { OverlayContainer } from '@angular/cdk/overlay';
import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { HeaderActionsOverflowComponent } from './header-actions-overflow';

@Component({
    imports: [HeaderActionsOverflowComponent],
    template: `
        <fd-header-actions-overflow>
            <button
                type="button"
                aria-label="First action"
                [attr.data-fd-overflow-primary]="primary() ? true : null"
                (click)="recordFirstAction()"
            >
                <span class="fd-ui-icon__glyph">edit</span>
            </button>

            @if (showSecond()) {
                <button type="button" aria-label="Second action" (click)="secondClicks.set(secondClicks() + 1)">
                    <span class="fd-ui-icon__glyph">delete</span>
                </button>
            }
        </fd-header-actions-overflow>
    `,
})
class TestHostComponent {
    public readonly primary = signal(false);
    public readonly showSecond = signal(false);
    public readonly firstClicks = signal(0);
    public readonly secondClicks = signal(0);
    public firstActionFocus: Element | null = null;

    public recordFirstAction(): void {
        this.firstActionFocus = document.activeElement;
        this.firstClicks.update(value => value + 1);
    }
}

describe('HeaderActionsOverflowComponent', () => {
    let fixture: ComponentFixture<TestHostComponent>;
    let component: TestHostComponent;
    let overlayRoot: HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
            providers: [provideTranslateTesting(), provideRouter([])],
        }).compileComponents();

        overlayRoot = TestBed.inject(OverlayContainer).getContainerElement();
        fixture = TestBed.createComponent(TestHostComponent);
        component = fixture.componentInstance;
    });

    async function settleAsync(): Promise<void> {
        fixture.detectChanges();
        await fixture.whenStable();
        await new Promise(resolve => setTimeout(resolve, 0));
        fixture.detectChanges();
    }

    function getOverflowTrigger(): HTMLButtonElement | null {
        const host = fixture.nativeElement as HTMLElement;

        return host.querySelector<HTMLButtonElement>('.fd-header-actions-overflow__trigger button');
    }

    function getMenuItems(): HTMLButtonElement[] {
        return Array.from(overlayRoot.querySelectorAll<HTMLButtonElement>('.fd-ui-menu__item'));
    }

    it('keeps a single header action unchanged', async () => {
        await settleAsync();

        expect(getOverflowTrigger()).toBeNull();
    });

    it('renders multiple header actions as menu items', async () => {
        component['showSecond'].set(true);
        await settleAsync();

        const trigger = getOverflowTrigger();
        expect(trigger).not.toBeNull();

        trigger?.click();
        await settleAsync();

        const items = getMenuItems();
        expect(items).toHaveLength(2);
        expect(items.map(item => item.textContent.trim())).toEqual(['editFirst action', 'deleteSecond action']);
    });

    it('keeps the primary action directly available and excludes it from the menu', async () => {
        component.primary.set(true);
        component.showSecond.set(true);
        await settleAsync();
        const host = fixture.nativeElement as HTMLElement;
        const primary = host.querySelector<HTMLButtonElement>('.fd-header-actions-overflow__primary button');
        expect(primary).not.toBeNull();
        primary?.click();
        await settleAsync();
        expect(component.firstClicks()).toBe(1);
        getOverflowTrigger()?.click();
        await settleAsync();
        expect(getMenuItems().map(item => item.textContent.trim())).toEqual(['deleteSecond action']);
    });

    it('uses visible action text without decorative icon glyphs for compact buttons', async () => {
        component.primary.set(true);
        component.showSecond.set(true);
        await settleAsync();
        const host = fixture.nativeElement as HTMLElement;
        const original = host.querySelector<HTMLButtonElement>('[data-fd-overflow-primary]');
        original?.removeAttribute('aria-label');
        original?.insertAdjacentHTML('beforeend', '<span>New list</span><span aria-hidden="true">hidden hint</span>');
        await settleAsync();

        const primary = host.querySelector<HTMLButtonElement>('.fd-header-actions-overflow__primary button');
        expect(primary?.getAttribute('aria-label')).toBe('New list');
        expect(primary?.querySelector('.fd-ui-icon__glyph')?.textContent).toBe('edit');
    });

    it('proxies menu item clicks to the original action', async () => {
        component['showSecond'].set(true);
        await settleAsync();

        getOverflowTrigger()?.click();
        await settleAsync();

        const firstItem = getMenuItems()[0];
        firstItem.click();
        await settleAsync();

        expect(component['firstClicks']()).toBe(1);
        expect(component['secondClicks']()).toBe(0);
    });
});

describe('Header action focus handoff', () => {
    it('focuses the stable trigger before launching a temporary menu action', async () => {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
            providers: [provideTranslateTesting(), provideRouter([])],
        }).compileComponents();
        const fixture = TestBed.createComponent(TestHostComponent);
        fixture.componentInstance.showSecond.set(true);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        const trigger = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
            '.fd-header-actions-overflow__trigger button',
        );
        trigger?.click();
        fixture.detectChanges();
        await fixture.whenStable();
        const item = TestBed.inject(OverlayContainer).getContainerElement().querySelector<HTMLButtonElement>('.fd-ui-menu__item');
        item?.focus();
        item?.click();
        expect(fixture.componentInstance.firstActionFocus).toBe(trigger);
        expect(fixture.componentInstance.firstClicks()).toBe(1);
    });
});
