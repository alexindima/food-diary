import { OverlayContainer } from '@angular/cdk/overlay';
import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { FdTourTargetRegistry } from 'fd-tour';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { HeaderActionsOverflowComponent } from './header-actions-overflow';

@Component({
    imports: [HeaderActionsOverflowComponent],
    template: `
        <fd-header-actions-overflow>
            <button
                type="button"
                aria-label="First action"
                [attr.aria-disabled]="firstActionDisabled() || null"
                [attr.data-fd-overflow-primary]="primary() ? true : null"
                (click)="recordFirstAction()"
            >
                <span class="fd-ui-icon__glyph">edit</span>
            </button>

            @if (showSecond()) {
                <button
                    type="button"
                    aria-label="Second action"
                    data-tour-id="secondary-action"
                    (click)="secondClicks.set(secondClicks() + 1)"
                >
                    <span class="fd-ui-icon__glyph">delete</span>
                </button>
            }
            <a [attr.href]="activityHref()">Open activity</a>
        </fd-header-actions-overflow>
    `,
})
class TestHostComponent {
    public readonly primary = signal(false);
    public readonly showSecond = signal(false);
    public readonly firstActionDisabled = signal(false);
    public readonly activityHref = signal<string | null>(null);
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

    function getOverflowTrigger(): HTMLButtonElement | null {
        const host = fixture.nativeElement as HTMLElement;

        return host.querySelector<HTMLButtonElement>('.fd-header-actions-overflow__trigger button');
    }

    function getMenuItems(): HTMLButtonElement[] {
        return Array.from(overlayRoot.querySelectorAll<HTMLButtonElement>('.fd-ui-menu__item'));
    }

    it('keeps a single header action unchanged', async () => {
        await settleHeaderActionsAsync(fixture);

        expect(getOverflowTrigger()).toBeNull();
    });

    it('renders multiple header actions as menu items', async () => {
        component['showSecond'].set(true);
        await settleHeaderActionsAsync(fixture);

        const trigger = getOverflowTrigger();
        expect(trigger).not.toBeNull();

        trigger?.click();
        await settleHeaderActionsAsync(fixture);

        const items = getMenuItems();
        expect(items).toHaveLength(2);
        expect(items.map(item => item.textContent.trim())).toEqual(['editFirst action', 'deleteSecond action']);
    });

    it('keeps the primary action directly available and excludes it from the menu', async () => {
        component.primary.set(true);
        component.showSecond.set(true);
        await settleHeaderActionsAsync(fixture);
        const host = fixture.nativeElement as HTMLElement;
        const primary = host.querySelector<HTMLButtonElement>('.fd-header-actions-overflow__primary button');
        expect(primary).not.toBeNull();
        primary?.click();
        await settleHeaderActionsAsync(fixture);
        expect(component.firstClicks()).toBe(1);
        getOverflowTrigger()?.click();
        await settleHeaderActionsAsync(fixture);
        expect(getMenuItems().map(item => item.textContent.trim())).toEqual(['deleteSecond action']);
    });

    it('uses visible action text without decorative icon glyphs for compact buttons', async () => {
        component.primary.set(true);
        component.showSecond.set(true);
        await settleHeaderActionsAsync(fixture);
        const host = fixture.nativeElement as HTMLElement;
        const original = host.querySelector<HTMLButtonElement>('[data-fd-overflow-primary]');
        original?.removeAttribute('aria-label');
        original?.insertAdjacentHTML('beforeend', '<span>New list</span><span aria-hidden="true">hidden hint</span>');
        await settleHeaderActionsAsync(fixture);

        const primary = host.querySelector<HTMLButtonElement>('.fd-header-actions-overflow__primary button');
        expect(primary?.getAttribute('aria-label')).toBe('New list');
        expect(primary?.querySelector('.fd-ui-icon__glyph')?.textContent).toBe('edit');
    });

    it('proxies menu item clicks to the original action', async () => {
        component['showSecond'].set(true);
        await settleHeaderActionsAsync(fixture);

        getOverflowTrigger()?.click();
        await settleHeaderActionsAsync(fixture);

        const firstItem = getMenuItems()[0];
        firstItem.click();
        await settleHeaderActionsAsync(fixture);

        expect(component['firstClicks']()).toBe(1);
        expect(component['secondClicks']()).toBe(0);
    });
});

it('does not activate a projected action disabled while its menu proxy is still rendered', async () => {
    await TestBed.configureTestingModule({
        imports: [TestHostComponent],
        providers: [provideTranslateTesting(), provideRouter([])],
    }).compileComponents();
    const fixture = TestBed.createComponent(TestHostComponent);
    const host = fixture.nativeElement as HTMLElement;
    const component = fixture.componentInstance;
    component.showSecond.set(true);
    await settleHeaderActionsAsync(fixture);
    host.querySelector<HTMLButtonElement>('.fd-header-actions-overflow__trigger button')?.click();
    await settleHeaderActionsAsync(fixture);
    const overlay = TestBed.inject(OverlayContainer).getContainerElement();
    const firstAction = Array.from(overlay.querySelectorAll<HTMLButtonElement>('.fd-ui-menu__item')).find(button =>
        button.textContent.includes('First action'),
    );

    component.firstActionDisabled.set(true);
    fixture.detectChanges();
    expect(host.querySelector('button[aria-label="First action"]')?.getAttribute('aria-disabled')).toBe('true');
    expect(firstAction?.isConnected).toBe(true);
    expect(firstAction?.disabled).toBe(false);
    firstAction?.click();

    expect(component.firstClicks()).toBe(0);
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
        const triggerHost = trigger?.closest<HTMLElement>('.fd-header-actions-overflow__trigger');
        if (trigger === null || triggerHost === null || triggerHost === undefined) {
            throw new Error('Expected the overflow trigger');
        }
        // jsdom does not evaluate the media query that reveals the compact trigger.
        triggerHost.style.display = 'inline-flex';
        trigger.focus();
        expect(document.activeElement).toBe(trigger);
        trigger.click();
        fixture.detectChanges();
        await fixture.whenStable();
        const item = TestBed.inject(OverlayContainer).getContainerElement().querySelector<HTMLButtonElement>('.fd-ui-menu__item');
        item?.focus();
        item?.click();
        expect(fixture.componentInstance.firstActionFocus).toBe(trigger);
        expect(fixture.componentInstance.firstClicks()).toBe(1);
    });
});

describe('Header action reactive metadata', () => {
    it('refreshes an existing text node and explicit action metadata', async () => {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
            providers: [provideTranslateTesting(), provideRouter([])],
        }).compileComponents();
        const fixture = TestBed.createComponent(TestHostComponent);
        const component = fixture.componentInstance;
        component.primary.set(true);
        component.showSecond.set(true);
        await settleHeaderActionsAsync(fixture);
        const host = fixture.nativeElement as HTMLElement;
        const original = host.querySelector<HTMLButtonElement>('[data-fd-overflow-primary]');
        if (original === null) {
            throw new Error('Expected the primary action');
        }
        const primary = (): HTMLButtonElement | null => host.querySelector('.fd-header-actions-overflow__primary button');
        original.removeAttribute('aria-label');
        original.setAttribute('title', 'Edit details');
        await settleHeaderActionsAsync(fixture);
        expect(primary()?.getAttribute('aria-label')).toBe('Edit details');

        const text = document.createTextNode('Before');
        original.append(text);
        await settleHeaderActionsAsync(fixture);
        text.data = 'After';
        await settleHeaderActionsAsync(fixture);
        expect(primary()?.getAttribute('aria-label')).toBe('After');
        original.setAttribute('data-fd-overflow-label', 'Translated label');
        original.setAttribute('data-fd-overflow-icon', 'add');
        await settleHeaderActionsAsync(fixture);
        expect(primary()?.getAttribute('aria-label')).toBe('Translated label');
        expect(primary()?.querySelector('.fd-ui-icon__glyph')?.textContent).toBe('add');
        original.setAttribute('disabled', '');
        original.setAttribute('title', 'Busy');
        await settleHeaderActionsAsync(fixture);
        original.setAttribute('title', 'Updated reason');
        await settleHeaderActionsAsync(fixture);
        expect(primary()?.disabled).toBe(true);
        expect(primary()?.title).toBe('Updated reason');
    });
});

describe('Header overflow links', () => {
    it('adds and removes a projected link when its destination changes', async () => {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
            providers: [provideTranslateTesting(), provideRouter([])],
        }).compileComponents();
        const fixture = TestBed.createComponent(TestHostComponent);
        const host = fixture.nativeElement as HTMLElement;
        const overlay = TestBed.inject(OverlayContainer).getContainerElement();
        const trigger = (): HTMLButtonElement | null => host.querySelector('.fd-header-actions-overflow__trigger button');
        await settleHeaderActionsAsync(fixture);
        expect(trigger()).toBeNull();

        fixture.componentInstance.activityHref.set('/activity');
        await settleHeaderActionsAsync(fixture);

        const menuButton = trigger();
        expect(menuButton).not.toBeNull();
        menuButton?.click();
        await settleHeaderActionsAsync(fixture);
        expect(Array.from(overlay.querySelectorAll('.fd-ui-menu__item')).map(item => item.textContent.trim())).toContain('Open activity');

        fixture.componentInstance.activityHref.set(null);
        await settleHeaderActionsAsync(fixture);
        expect(trigger()).toBeNull();
        expect(overlay.querySelector('.fd-ui-menu__item')).toBeNull();
    });
});

const SECONDARY_TOUR_TARGET = '[data-tour-id="secondary-action"]';

describe('Header overflow guided tour', () => {
    let fixture: ComponentFixture<TestHostComponent>;
    let registry: FdTourTargetRegistry;
    let overlay: HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
            providers: [provideTranslateTesting(), provideRouter([])],
        }).compileComponents();
        fixture = TestBed.createComponent(TestHostComponent);
        registry = TestBed.inject(FdTourTargetRegistry);
        overlay = TestBed.inject(OverlayContainer).getContainerElement();
    });

    it('only offers a resolver when the requested action can be revealed through a visible trigger', async () => {
        await settleHeaderActionsAsync(fixture);
        expect(registry.find(SECONDARY_TOUR_TARGET)).toBeUndefined();
        fixture.componentInstance.showSecond.set(true);
        await settleHeaderActionsAsync(fixture);
        const trigger = overflowHost(fixture);
        trigger.style.visibility = 'hidden';

        expect(registry.find(SECONDARY_TOUR_TARGET)).toBeUndefined();
        trigger.style.visibility = 'visible';
        expect(registry.find(SECONDARY_TOUR_TARGET)).toBeDefined();
        fixture.destroy();
        expect(registry.find(SECONDARY_TOUR_TARGET)).toBeUndefined();
    });

    it('scrolls to the trigger and closes only the menu it opened to reveal the target', async () => {
        fixture.componentInstance.showSecond.set(true);
        await settleHeaderActionsAsync(fixture);
        const trigger = overflowHost(fixture);
        const scrollIntoView = vi.fn();
        Object.defineProperty(trigger, 'scrollIntoView', { value: scrollIntoView, configurable: true });
        const resolver = registry.find(SECONDARY_TOUR_TARGET);
        if (resolver === undefined) {
            throw new Error('Expected the visible overflow to resolve the tour target');
        }

        const release = resolver.reveal(SECONDARY_TOUR_TARGET);
        await settleHeaderActionsAsync(fixture);
        expect(scrollIntoView).toHaveBeenCalledExactlyOnceWith({ block: 'nearest', inline: 'nearest', behavior: 'instant' });
        expect(overlay.querySelector(SECONDARY_TOUR_TARGET)?.textContent).toContain('Second action');
        expect(fixture.componentInstance.secondClicks()).toBe(0);

        release();
        await settleHeaderActionsAsync(fixture);
        expect(overlay.querySelector(SECONDARY_TOUR_TARGET)).toBeNull();

        trigger.querySelector('button')?.click();
        await settleHeaderActionsAsync(fixture);
        expect(overlay.querySelector(SECONDARY_TOUR_TARGET)).not.toBeNull();
        resolver.reveal(SECONDARY_TOUR_TARGET)();
        await settleHeaderActionsAsync(fixture);
        expect(overlay.querySelector(SECONDARY_TOUR_TARGET)).not.toBeNull();
        expect(fixture.componentInstance.secondClicks()).toBe(0);
    });
});

it('keeps a keyboard-opened overflow in its local tour scope and disposes it with the header', async () => {
    await TestBed.configureTestingModule({
        imports: [TestHostComponent],
        providers: [provideTranslateTesting(), provideRouter([])],
    })
        .overrideComponent(HeaderActionsOverflowComponent, { add: { providers: [FdTourTargetRegistry] } })
        .compileComponents();

    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.componentInstance.showSecond.set(true);
    await settleHeaderActionsAsync(fixture);

    const trigger = overflowHost(fixture).querySelector('button');
    if (trigger === null) {
        throw new Error('Expected the header menu trigger');
    }

    const header = fixture.debugElement.query(By.directive(HeaderActionsOverflowComponent));
    const registry = header.injector.get(FdTourTargetRegistry);
    const applicationRegistry = TestBed.inject(FdTourTargetRegistry);
    const resolver = registry.find(SECONDARY_TOUR_TARGET);
    expect(applicationRegistry.find(SECONDARY_TOUR_TARGET)).toBeUndefined();
    if (resolver === undefined) {
        throw new Error('Expected the header to register its own tour target');
    }

    const overlay = TestBed.inject(OverlayContainer).getContainerElement();
    trigger.focus();
    trigger.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }));
    await settleHeaderActionsAsync(fixture);
    const firstAction = overlay.querySelector('[role="menuitem"]');
    expect(firstAction?.textContent).toContain('First action');
    expect(document.activeElement).toBe(firstAction);

    resolver.reveal(SECONDARY_TOUR_TARGET)();
    await settleHeaderActionsAsync(fixture);
    expect(overlay.querySelector(SECONDARY_TOUR_TARGET)?.textContent).toContain('Second action');
    expect(document.activeElement).toBe(firstAction);

    firstAction?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));
    await settleHeaderActionsAsync(fixture);
    expect(overlay.querySelector('[role="menu"]')).toBeNull();
    expect(document.activeElement).toBe(trigger);
    expect(fixture.componentInstance.secondClicks()).toBe(0);

    resolver.reveal(SECONDARY_TOUR_TARGET);
    await settleHeaderActionsAsync(fixture);
    expect(overlay.querySelector(SECONDARY_TOUR_TARGET)).not.toBeNull();
    fixture.destroy();
    expect(overlay.querySelector('[role="menu"]')).toBeNull();
    expect(registry.resolvers()).toEqual([]);
});

function overflowHost(fixture: ComponentFixture<TestHostComponent>): HTMLElement {
    const host = fixture.nativeElement as HTMLElement;
    const trigger = host.querySelector<HTMLElement>('.fd-header-actions-overflow__trigger');
    if (trigger === null) {
        throw new Error('Expected the rendered overflow trigger');
    }
    trigger.style.display = 'inline-flex';
    return trigger;
}

async function settleHeaderActionsAsync(fixture: ComponentFixture<TestHostComponent>): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    await new Promise(resolve => setTimeout(resolve, 0));
    fixture.detectChanges();
}
