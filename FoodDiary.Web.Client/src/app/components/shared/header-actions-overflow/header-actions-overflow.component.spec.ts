import { OverlayContainer } from '@angular/cdk/overlay';
import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import { HeaderActionsOverflowComponent } from './header-actions-overflow.component';

@Component({
    imports: [HeaderActionsOverflowComponent],
    template: `
        <fd-header-actions-overflow>
            <button type="button" aria-label="First action" (click)="firstClicks.set(firstClicks() + 1)">
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
    public readonly showSecond = signal(false);
    public readonly firstClicks = signal(0);
    public readonly secondClicks = signal(0);
}

describe('HeaderActionsOverflowComponent', () => {
    let fixture: ComponentFixture<TestHostComponent>;
    let component: TestHostComponent;
    let overlayRoot: HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent, TranslateModule.forRoot()],
            providers: [provideRouter([])],
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
        component.showSecond.set(true);
        await settleAsync();

        const trigger = getOverflowTrigger();
        expect(trigger).not.toBeNull();

        trigger?.click();
        await settleAsync();

        const items = getMenuItems();
        expect(items).toHaveLength(2);
        expect(items.map(item => item.textContent.trim())).toEqual(['editFirst action', 'deleteSecond action']);
    });

    it('proxies menu item clicks to the original action', async () => {
        component.showSecond.set(true);
        await settleAsync();

        getOverflowTrigger()?.click();
        await settleAsync();

        const firstItem = getMenuItems()[0];
        firstItem.click();
        await settleAsync();

        expect(component.firstClicks()).toBe(1);
        expect(component.secondClicks()).toBe(0);
    });
});
