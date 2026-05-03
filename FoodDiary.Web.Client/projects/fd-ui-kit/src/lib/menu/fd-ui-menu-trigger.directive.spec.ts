import { OverlayContainer } from '@angular/cdk/overlay';
import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiMenuComponent } from './fd-ui-menu.component';
import { FdUiMenuItemComponent } from './fd-ui-menu-item.component';
import { FdUiMenuTriggerDirective } from './fd-ui-menu-trigger.directive';

@Component({
    standalone: true,
    imports: [FdUiMenuTriggerDirective, FdUiMenuComponent, FdUiMenuItemComponent],
    template: `
        <button type="button" [fdUiMenuTrigger]="menu">Actions</button>

        <fd-ui-menu #menu="fdUiMenu">
            <fd-ui-menu-item>First</fd-ui-menu-item>
            <fd-ui-menu-item [disabled]="true">Disabled</fd-ui-menu-item>
            <fd-ui-menu-item>Last</fd-ui-menu-item>
        </fd-ui-menu>
    `,
})
class TestHostComponent {}

describe('FdUiMenuTriggerDirective', () => {
    let fixture: ComponentFixture<TestHostComponent>;
    let overlayContainer: OverlayContainer;
    let overlayRoot: HTMLElement;
    let trigger: HTMLButtonElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
            providers: [provideNoopAnimations(), provideRouter([])],
        }).compileComponents();

        overlayContainer = TestBed.inject(OverlayContainer);
        overlayRoot = overlayContainer.getContainerElement();

        fixture = TestBed.createComponent(TestHostComponent);
        fixture.detectChanges();

        trigger = fixture.nativeElement.querySelector('button');
    });

    async function flushOverlayFocus(): Promise<void> {
        fixture.detectChanges();
        await fixture.whenStable();
        await Promise.resolve();
        await new Promise(resolve => setTimeout(resolve, 0));
        fixture.detectChanges();
    }

    function dispatchTriggerKey(key: string): void {
        trigger.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
    }

    function getMenuItems(): HTMLButtonElement[] {
        return Array.from(overlayRoot.querySelectorAll('.fd-ui-menu__item'));
    }

    it('opens with ArrowUp and focuses the last enabled item', async () => {
        const menu = fixture.debugElement.query(By.directive(FdUiMenuComponent)).componentInstance as FdUiMenuComponent;
        const focusLastItem = vi.spyOn(menu, 'focusLastItem');

        dispatchTriggerKey('ArrowUp');
        await flushOverlayFocus();

        const items = getMenuItems();
        expect(items).toHaveLength(3);
        expect(focusLastItem).toHaveBeenCalledOnce();
    });

    it('closes on Escape and restores focus to the trigger', async () => {
        dispatchTriggerKey('ArrowDown');
        await flushOverlayFocus();

        const firstItem = getMenuItems()[0];
        firstItem.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
        await flushOverlayFocus();

        expect(getMenuItems()).toHaveLength(0);
        expect(document.activeElement).toBe(trigger);
    });

    it('closes on Tab without restoring focus to the trigger', async () => {
        dispatchTriggerKey('ArrowDown');
        await flushOverlayFocus();

        const firstItem = getMenuItems()[0];
        firstItem.focus();
        firstItem.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true }));
        await flushOverlayFocus();

        expect(getMenuItems()).toHaveLength(0);
        expect(document.activeElement).not.toBe(trigger);
    });
});
