import { OverlayContainer } from '@angular/cdk/overlay';
import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../src/testing/async-testing';
import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiMenuComponent } from './fd-ui-menu';
import { FdUiMenuItemComponent } from './fd-ui-menu-item';
import { FdUiMenuTriggerDirective } from './fd-ui-menu-trigger.directive';

const MENU_ITEM_COUNT = 3;

@Component({
    imports: [FdUiButtonComponent, FdUiMenuTriggerDirective, FdUiMenuComponent, FdUiMenuItemComponent],
    template: `
        @if (uiKitButton) {
            <fd-ui-button
                [ariaExpanded]="explicitExpanded()"
                [ariaHasPopup]="explicitPopup()"
                [fdUiMenuTrigger]="menuEnabled() ? menu : null"
                >Actions</fd-ui-button
            >
        } @else {
            <button type="button" [fdUiMenuTrigger]="menuEnabled() ? menu : null">Actions</button>
        }

        <fd-ui-menu #menu="fdUiMenu">
            <fd-ui-menu-item>First</fd-ui-menu-item>
            <fd-ui-menu-item [disabled]="true">Disabled</fd-ui-menu-item>
            <fd-ui-menu-item>Last</fd-ui-menu-item>
        </fd-ui-menu>
    `,
})
class TestHostComponent {
    public uiKitButton = false;
    public readonly menuEnabled = signal(true);
    public readonly explicitExpanded = signal<boolean | null>(null);
    public readonly explicitPopup = signal<'dialog' | null>(null);
}

async function flushOverlayFocusAsync(fixture: ComponentFixture<TestHostComponent>): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    await waitForAsyncTasksAsync();
    await new Promise(resolve => setTimeout(resolve, 0));
    fixture.detectChanges();
}

describe.each([false, true])('FdUiMenuTriggerDirective with UI kit trigger %s', uiKitButton => {
    let fixture: ComponentFixture<TestHostComponent>;
    let overlayContainer: OverlayContainer;
    let overlayRoot: HTMLElement;
    let trigger: HTMLButtonElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
            providers: [provideRouter([])],
        }).compileComponents();

        overlayContainer = TestBed.inject(OverlayContainer);
        overlayRoot = overlayContainer.getContainerElement();

        fixture = TestBed.createComponent(TestHostComponent);
        fixture.componentInstance.uiKitButton = uiKitButton;
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const button = host.querySelector<HTMLButtonElement>('button');
        if (button === null) {
            throw new Error('Expected menu trigger to exist.');
        }

        trigger = button;
    });

    function getMenuItems(): HTMLButtonElement[] {
        return Array.from(overlayRoot.querySelectorAll<HTMLButtonElement>('.fd-ui-menu__item'));
    }

    it('publishes menu semantics and expanded state on the focusable native trigger', async () => {
        await verifyMenuAriaAsync(fixture, trigger);
    });

    it('opens with ArrowUp and focuses the last enabled item', async () => {
        const menu = fixture.debugElement.query(By.directive(FdUiMenuComponent)).componentInstance as FdUiMenuComponent;
        const focusLastItem = vi.spyOn(menu, 'focusLastItem');

        dispatchTriggerKey(trigger, 'ArrowUp');
        await flushOverlayFocusAsync(fixture);

        const items = getMenuItems();
        expect(items).toHaveLength(MENU_ITEM_COUNT);
        expect(focusLastItem).toHaveBeenCalledOnce();
        expect(document.activeElement).toBe(items[2]);
    });

    it('moves focus between enabled native menu item buttons', async () => {
        dispatchTriggerKey(trigger, 'ArrowDown');
        await flushOverlayFocusAsync(fixture);
        const items = getMenuItems();
        expect(document.activeElement).toBe(items[0]);
        items[0].dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
        expect(document.activeElement).toBe(items[2]);
        items[2].dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
        expect(document.activeElement).toBe(items[0]);
        items[0].dispatchEvent(new KeyboardEvent('keydown', { key: 'End', bubbles: true }));
        expect(document.activeElement).toBe(items[2]);
        items[2].dispatchEvent(new KeyboardEvent('keydown', { key: 'Home', bubbles: true }));
        expect(document.activeElement).toBe(items[0]);
    });

    it('restores trigger focus after selecting an item', async () => {
        dispatchTriggerKey(trigger, 'ArrowDown');
        await flushOverlayFocusAsync(fixture);
        getMenuItems()[0].click();
        await flushOverlayFocusAsync(fixture);
        expect(getMenuItems()).toHaveLength(0);
        expect(document.activeElement).toBe(trigger);
    });

    it('closes on Escape and restores focus to the trigger', async () => {
        dispatchTriggerKey(trigger, 'ArrowDown');
        await flushOverlayFocusAsync(fixture);

        const firstItem = getMenuItems()[0];
        firstItem.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
        await flushOverlayFocusAsync(fixture);

        expect(getMenuItems()).toHaveLength(0);
        expect(document.activeElement).toBe(trigger);
    });

    it.each([false, true])('anchors default Tab movement on the trigger (shift=%s)', async shiftKey => {
        await verifyTabDismissalAsync(fixture, trigger, overlayRoot, shiftKey);
    });
});

async function verifyTabDismissalAsync(
    fixture: ComponentFixture<TestHostComponent>,
    trigger: HTMLButtonElement,
    overlayRoot: HTMLElement,
    shiftKey: boolean,
): Promise<void> {
    dispatchTriggerKey(trigger, 'ArrowDown');
    await flushOverlayFocusAsync(fixture);
    const firstItem = overlayRoot.querySelector<HTMLButtonElement>('.fd-ui-menu__item');
    expect(firstItem).not.toBeNull();
    firstItem?.focus();
    const event = new KeyboardEvent('keydown', { key: 'Tab', shiftKey, bubbles: true, cancelable: true });
    firstItem?.dispatchEvent(event);
    await flushOverlayFocusAsync(fixture);
    expect(overlayRoot.querySelectorAll('.fd-ui-menu__item')).toHaveLength(0);
    expect(event.defaultPrevented).toBe(false);
    expect(document.activeElement).toBe(trigger);
}

async function verifyMenuAriaAsync(fixture: ComponentFixture<TestHostComponent>, trigger: HTMLButtonElement): Promise<void> {
    await flushOverlayFocusAsync(fixture);
    expect(trigger.getAttribute('aria-haspopup')).toBe('menu');
    expect(trigger.getAttribute('aria-expanded')).toBe('false');
    trigger.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    await flushOverlayFocusAsync(fixture);
    expect(trigger.getAttribute('aria-expanded')).toBe('true');
    TestBed.inject(OverlayContainer)
        .getContainerElement()
        .querySelector<HTMLButtonElement>('.fd-ui-menu__item')
        ?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await flushOverlayFocusAsync(fixture);
    expect(trigger.getAttribute('aria-expanded')).toBe('false');
    fixture.componentInstance.explicitExpanded.set(true);
    fixture.componentInstance.explicitPopup.set('dialog');
    dispatchTriggerKey(trigger, 'ArrowDown');
    await flushOverlayFocusAsync(fixture);
    fixture.componentInstance.menuEnabled.set(false);
    await flushOverlayFocusAsync(fixture);
    const isUiKit = fixture.componentInstance.uiKitButton;
    expect(trigger.getAttribute('aria-haspopup')).toBe(isUiKit ? 'dialog' : null);
    expect(trigger.getAttribute('aria-expanded')).toBe(isUiKit ? 'true' : null);
    expect(TestBed.inject(OverlayContainer).getContainerElement().querySelector('.fd-ui-menu__item')).toBeNull();
}

function dispatchTriggerKey(trigger: HTMLButtonElement, key: string): void {
    trigger.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
}
