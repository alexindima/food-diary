import { OverlayContainer } from '@angular/cdk/overlay';
import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../src/testing/async-testing';
import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiMenuComponent } from './fd-ui-menu';
import { FdUiMenuDividerComponent } from './fd-ui-menu-divider';
import { FdUiMenuItemComponent } from './fd-ui-menu-item';
import { FdUiMenuTriggerDirective } from './fd-ui-menu-trigger.directive';

const MENU_ITEM_COUNT = 3;

@Component({
    imports: [FdUiButtonComponent, FdUiMenuTriggerDirective, FdUiMenuComponent, FdUiMenuDividerComponent, FdUiMenuItemComponent],
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
        <button class="outside-action" type="button">Outside action</button>

        <fd-ui-menu #menu="fdUiMenu">
            <fd-ui-menu-item [disabled]="disableAllItems()" (itemClick)="keepOpenWhenRequested($event)">First</fd-ui-menu-item>
            <fd-ui-menu-item [disabled]="true">Disabled</fd-ui-menu-item>
            <fd-ui-menu-divider />
            <fd-ui-menu-item [disabled]="disableAllItems()">Last</fd-ui-menu-item>
        </fd-ui-menu>
    `,
})
class TestHostComponent {
    public uiKitButton = false;
    public readonly menuEnabled = signal(true);
    public readonly explicitExpanded = signal<boolean | null>(null);
    public readonly explicitPopup = signal<'dialog' | null>(null);
    public readonly disableAllItems = signal(false);
    public readonly keepOpen = signal(false);

    protected keepOpenWhenRequested(event: Event): void {
        if (this.keepOpen()) {
            event.preventDefault();
        }
    }
}

async function flushOverlayFocusAsync(fixture: ComponentFixture<TestHostComponent>): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    await waitForAsyncTasksAsync();
    await new Promise(resolve => setTimeout(resolve, 0));
    fixture.detectChanges();
}

type MenuFixture = {
    fixture: ComponentFixture<TestHostComponent>;
    overlayRoot: HTMLElement;
    trigger: HTMLButtonElement;
};

async function setUpMenuAsync(uiKitButton: boolean): Promise<MenuFixture> {
    await TestBed.configureTestingModule({
        imports: [TestHostComponent],
        providers: [provideRouter([])],
    }).compileComponents();

    const overlayRoot = TestBed.inject(OverlayContainer).getContainerElement();
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.componentInstance.uiKitButton = uiKitButton;
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const trigger = host.querySelector('button');
    if (trigger === null) {
        throw new Error('Expected menu trigger to exist.');
    }

    return { fixture, overlayRoot, trigger };
}

function menuItems(overlayRoot: HTMLElement): HTMLButtonElement[] {
    return Array.from(overlayRoot.querySelectorAll<HTMLButtonElement>('.fd-ui-menu__item'));
}

describe.each([false, true])('FdUiMenuTriggerDirective with UI kit trigger %s', uiKitButton => {
    let fixture: ComponentFixture<TestHostComponent>;
    let overlayRoot: HTMLElement;
    let trigger: HTMLButtonElement;

    beforeEach(async () => {
        ({ fixture, overlayRoot, trigger } = await setUpMenuAsync(uiKitButton));
    });

    function getMenuItems(): HTMLButtonElement[] {
        return menuItems(overlayRoot);
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
        items[0].dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }));
        expect(document.activeElement).toBe(items[2]);
        expect(overlayRoot.querySelector('.fd-ui-menu__divider')).not.toBeNull();
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

describe.each([false, true])('FdUiMenuTriggerDirective dismissal with UI kit trigger %s', uiKitButton => {
    it('opens and closes from the trigger and can reopen after a backdrop click', async () => {
        const { fixture, overlayRoot, trigger } = await setUpMenuAsync(uiKitButton);
        trigger.click();
        await flushOverlayFocusAsync(fixture);
        expect(menuItems(overlayRoot)).toHaveLength(MENU_ITEM_COUNT);
        expect(trigger.getAttribute('aria-expanded')).toBe('true');

        trigger.click();
        await flushOverlayFocusAsync(fixture);
        expect(menuItems(overlayRoot)).toHaveLength(0);
        expect(document.activeElement).toBe(trigger);

        trigger.click();
        await flushOverlayFocusAsync(fixture);
        const backdrop = overlayRoot.querySelector<HTMLElement>('.cdk-overlay-backdrop');
        expect(backdrop).toBeTruthy();
        backdrop?.click();
        await flushOverlayFocusAsync(fixture);
        expect(menuItems(overlayRoot)).toHaveLength(0);
        expect(document.activeElement).toBe(trigger);
        expect(trigger.getAttribute('aria-expanded')).toBe('false');

        trigger.click();
        await flushOverlayFocusAsync(fixture);
        expect(document.activeElement).toBe(menuItems(overlayRoot)[0]);
    });

    it('handles Escape on the trigger only while the menu is open', async () => {
        const { fixture, overlayRoot, trigger } = await setUpMenuAsync(uiKitButton);
        const pressEscape = (): KeyboardEvent => {
            const event = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
            trigger.dispatchEvent(event);
            return event;
        };

        expect(pressEscape().defaultPrevented).toBe(false);
        dispatchTriggerKey(trigger, 'Enter');
        await flushOverlayFocusAsync(fixture);
        expect(menuItems(overlayRoot)).toHaveLength(MENU_ITEM_COUNT);

        trigger.focus();
        expect(pressEscape().defaultPrevented).toBe(true);
        await flushOverlayFocusAsync(fixture);
        expect(menuItems(overlayRoot)).toHaveLength(0);
        expect(document.activeElement).toBe(trigger);
    });
});

describe('FdUiMenuTriggerDirective public overlay lifecycle', () => {
    it('keeps programmatic opening idempotent and handles Escape while focus remains outside', async () => {
        const { fixture, overlayRoot, trigger } = await setUpMenuAsync(false);
        const directive = fixture.debugElement.query(By.directive(FdUiMenuTriggerDirective)).injector.get(FdUiMenuTriggerDirective);
        const outside = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.outside-action');
        expect(outside).not.toBeNull();
        outside?.focus();

        directive.open('none');
        directive.open('last');
        await flushOverlayFocusAsync(fixture);
        expect(directive.isOpen()).toBe(true);
        expect(overlayRoot.querySelectorAll('[role="menu"]')).toHaveLength(1);
        expect(document.activeElement).toBe(outside);

        const escape = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
        outside?.dispatchEvent(escape);
        await flushOverlayFocusAsync(fixture);
        expect(escape.defaultPrevented).toBe(true);
        expect(directive.isOpen()).toBe(false);
        expect(document.activeElement).toBe(trigger);
    });

    it('cleans up if a container opens and closes before render without claiming focus', async () => {
        const { fixture, overlayRoot } = await setUpMenuAsync(false);
        const directive = fixture.debugElement.query(By.directive(FdUiMenuTriggerDirective)).injector.get(FdUiMenuTriggerDirective);
        const outside = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.outside-action');
        expect(outside).not.toBeNull();
        outside?.focus();

        directive.open('none');
        directive.close(false);
        directive.close();
        await flushOverlayFocusAsync(fixture);
        expect(directive.isOpen()).toBe(false);
        expect(overlayRoot.querySelector('[role="menu"]')).toBeNull();
        expect(document.activeElement).toBe(outside);

        fixture.componentInstance.menuEnabled.set(false);
        await flushOverlayFocusAsync(fixture);
        directive.open();
        expect(directive.isOpen()).toBe(false);
    });
});

describe('FdUiMenuComponent interactions without a selectable option', () => {
    it('leaves the menu open when the chosen action prevents the default close', async () => {
        const { fixture, overlayRoot, trigger } = await setUpMenuAsync(false);
        fixture.componentInstance.keepOpen.set(true);
        dispatchTriggerKey(trigger, 'ArrowDown');
        await flushOverlayFocusAsync(fixture);

        const [firstItem] = menuItems(overlayRoot);
        firstItem.click();
        await flushOverlayFocusAsync(fixture);
        expect(menuItems(overlayRoot)).toHaveLength(MENU_ITEM_COUNT);
        expect(document.activeElement).toBe(firstItem);
    });

    it.each(['ArrowDown', 'ArrowUp'])('keeps the trigger focus when %s opens a disabled menu', async key => {
        const { fixture, overlayRoot, trigger } = await setUpMenuAsync(false);
        fixture.componentInstance.disableAllItems.set(true);
        fixture.detectChanges();
        trigger.focus();
        dispatchTriggerKey(trigger, key);
        await flushOverlayFocusAsync(fixture);
        const menu = overlayRoot.querySelector('[role="menu"]');
        expect(menu).not.toBeNull();
        expect(menuItems(overlayRoot).every(item => item.disabled)).toBe(true);
        expect(document.activeElement).toBe(trigger);
        const down = new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true });
        menu?.dispatchEvent(down);
        expect(down.defaultPrevented).toBe(false);

        menu?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));
        await flushOverlayFocusAsync(fixture);
        expect(overlayRoot.querySelector('[role="menu"]')).toBeNull();
        expect(document.activeElement).toBe(trigger);
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
