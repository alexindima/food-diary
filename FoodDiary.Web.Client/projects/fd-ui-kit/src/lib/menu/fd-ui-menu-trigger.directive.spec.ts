import { OverlayContainer } from '@angular/cdk/overlay';
import { Component } from '@angular/core';
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
            <fd-ui-button [fdUiMenuTrigger]="menu">Actions</fd-ui-button>
        } @else {
            <button type="button" [fdUiMenuTrigger]="menu">Actions</button>
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

    function dispatchTriggerKey(key: string): void {
        trigger.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
    }

    function getMenuItems(): HTMLButtonElement[] {
        return Array.from(overlayRoot.querySelectorAll<HTMLButtonElement>('.fd-ui-menu__item'));
    }

    it('opens with ArrowUp and focuses the last enabled item', async () => {
        const menu = fixture.debugElement.query(By.directive(FdUiMenuComponent)).componentInstance as FdUiMenuComponent;
        const focusLastItem = vi.spyOn(menu, 'focusLastItem');

        dispatchTriggerKey('ArrowUp');
        await flushOverlayFocusAsync(fixture);

        const items = getMenuItems();
        expect(items).toHaveLength(MENU_ITEM_COUNT);
        expect(focusLastItem).toHaveBeenCalledOnce();
        expect(document.activeElement).toBe(items[2]);
    });

    it('moves focus between enabled native menu item buttons', async () => {
        dispatchTriggerKey('ArrowDown');
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
        dispatchTriggerKey('ArrowDown');
        await flushOverlayFocusAsync(fixture);
        getMenuItems()[0].click();
        await flushOverlayFocusAsync(fixture);
        expect(getMenuItems()).toHaveLength(0);
        expect(document.activeElement).toBe(trigger);
    });

    it('closes on Escape and restores focus to the trigger', async () => {
        dispatchTriggerKey('ArrowDown');
        await flushOverlayFocusAsync(fixture);

        const firstItem = getMenuItems()[0];
        firstItem.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
        await flushOverlayFocusAsync(fixture);

        expect(getMenuItems()).toHaveLength(0);
        expect(document.activeElement).toBe(trigger);
    });

    it('closes on Tab without restoring focus to the trigger', async () => {
        dispatchTriggerKey('ArrowDown');
        await flushOverlayFocusAsync(fixture);

        const firstItem = getMenuItems()[0];
        firstItem.focus();
        firstItem.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true }));
        await flushOverlayFocusAsync(fixture);

        expect(getMenuItems()).toHaveLength(0);
        expect(document.activeElement).not.toBe(trigger);
    });
});
