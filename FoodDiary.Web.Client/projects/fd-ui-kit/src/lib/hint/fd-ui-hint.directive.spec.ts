import '@angular/compiler';

import { OverlayContainer } from '@angular/cdk/overlay';
import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiHintDirective } from './fd-ui-hint.directive';

const HINT_SHOW_DELAY_MS = 500;
const HINT_HIDE_DELAY_MS = 300;
const ZERO_DELAY_MS = 0;

let overlayContainer: OverlayContainer | null = null;

@Component({
    imports: [FdUiHintDirective],
    template: `
        <button
            type="button"
            [fdUiHint]="hint"
            [fdUiHintContext]="hintContext"
            [fdUiHintDisabled]="disabled"
            [fdUiHintFocusShowDelay]="focusShowDelayMs"
            [fdUiHintHideDelay]="hideDelayMs"
            [fdUiHintHtml]="html"
            [fdUiHintPosition]="position"
            [fdUiHintShowDelay]="showDelayMs"
            [attr.aria-describedby]="describedBy"
        >
            Open dialog
        </button>
    `,
})
class TestHostComponent {
    public hint = 'Notifications';
    public hintContext: Record<string, unknown> | null = null;
    public disabled = false;
    public focusShowDelayMs = ZERO_DELAY_MS;
    public hideDelayMs = ZERO_DELAY_MS;
    public html = false;
    public position: 'top' | 'bottom' | 'left' | 'right' = 'bottom';
    public showDelayMs = HINT_SHOW_DELAY_MS;
    public describedBy: string | null = null;
}

@Component({
    imports: [FdUiHintDirective],
    template: '<button type="button" fdUiHint="Notifications" aria-describedby="external-description">Open dialog</button>',
})
class StaticDescriptionHostComponent {}

@Component({
    imports: [FdUiHintDirective],
    template: `
        <button type="button" [fdUiHint]="details" [fdUiHintContext]="{ $implicit: 'Template hint', name: 'Alex' }">Open dialog</button>

        <ng-template #details let-message let-name="name">{{ message }} for {{ name }}</ng-template>
    `,
})
class TemplateHintHostComponent {}

type HintTestContext<TComponent> = {
    fixture: ComponentFixture<TComponent>;
    overlayRoot: HTMLElement;
    trigger: HTMLButtonElement;
};

function requireTrigger(host: HTMLElement): HTMLButtonElement {
    const trigger = host.querySelector<HTMLButtonElement>('button');
    if (trigger === null) {
        throw new Error('Expected hint trigger to exist.');
    }

    return trigger;
}

function queryTooltip(overlayRoot: HTMLElement): HTMLElement | null {
    return overlayRoot.querySelector<HTMLElement>('.fd-ui-hint');
}

function tooltipText(overlayRoot: HTMLElement): string {
    const tooltip = queryTooltip(overlayRoot);
    return tooltip === null ? '' : tooltip.textContent.trim();
}

function dispatchTriggerEvent(trigger: HTMLElement, event: Event): void {
    trigger.dispatchEvent(event);
}

function showWithMouse<TComponent>(context: HintTestContext<TComponent>, delay = HINT_SHOW_DELAY_MS): void {
    dispatchTriggerEvent(context.trigger, new MouseEvent('mouseenter', { bubbles: true }));
    context.fixture.detectChanges();
    vi.advanceTimersByTime(delay);
    context.fixture.detectChanges();
}

async function createContextAsync<TComponent>(
    component: new () => TComponent,
    configure?: (component: TComponent) => void,
): Promise<HintTestContext<TComponent>> {
    await TestBed.configureTestingModule({
        imports: [component],
    }).compileComponents();

    overlayContainer = TestBed.inject(OverlayContainer);
    const fixture = TestBed.createComponent(component);
    configure?.(fixture.componentInstance);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    return {
        fixture,
        overlayRoot: overlayContainer.getContainerElement(),
        trigger: requireTrigger(host),
    };
}

describe('FdUiHintDirective', () => {
    beforeEach(() => {
        vi.useFakeTimers();
    });

    afterEach(() => {
        overlayContainer?.ngOnDestroy();
        vi.runOnlyPendingTimers();
        vi.useRealTimers();
    });

    registerDisplayTests();
    registerContentTests();
    registerDismissTests();
});

function registerDisplayTests(): void {
    describe('display', () => {
        it('shows tooltip text after the configured delay', async () => {
            const context = await createContextAsync(TestHostComponent);
            overlayContainer = TestBed.inject(OverlayContainer);

            showWithMouse(context);

            expect(tooltipText(context.overlayRoot)).toBe('Notifications');
            expect(context.trigger.getAttribute('aria-describedby')).toContain('fd-ui-hint-');
        });

        it('preserves existing described-by tokens while visible and restores them after hide', async () => {
            const context = await createContextAsync(StaticDescriptionHostComponent);

            showWithMouse(context);

            expect(context.trigger.getAttribute('aria-describedby')).toContain('external-description');
            expect(context.trigger.getAttribute('aria-describedby')).toContain('fd-ui-hint-');

            dispatchTriggerEvent(context.trigger, new MouseEvent('mouseleave', { bubbles: true }));
            context.fixture.detectChanges();
            vi.advanceTimersByTime(ZERO_DELAY_MS);
            context.fixture.detectChanges();

            expect(context.trigger.getAttribute('aria-describedby')).toBe('external-description');
        });

        it('does not show empty hints', async () => {
            const context = await createContextAsync(TestHostComponent, component => {
                component.hint = ' ';
            });

            showWithMouse(context);

            expect(queryTooltip(context.overlayRoot)).toBeNull();
        });

        it('does not show disabled hints', async () => {
            const context = await createContextAsync(TestHostComponent, component => {
                component.disabled = true;
            });

            showWithMouse(context);

            expect(queryTooltip(context.overlayRoot)).toBeNull();
        });

        it('shows on focus only for focus-visible hosts', async () => {
            const context = await createContextAsync(TestHostComponent);
            overlayContainer = TestBed.inject(OverlayContainer);
            const matchesSpy = vi.spyOn(context.trigger, 'matches');

            matchesSpy.mockReturnValue(false);
            dispatchTriggerEvent(context.trigger, new FocusEvent('focusin', { bubbles: true }));
            context.fixture.detectChanges();
            expect(queryTooltip(context.overlayRoot)).toBeNull();

            matchesSpy.mockReturnValue(true);
            dispatchTriggerEvent(context.trigger, new FocusEvent('focusin', { bubbles: true }));
            context.fixture.detectChanges();
            vi.advanceTimersByTime(ZERO_DELAY_MS);
            context.fixture.detectChanges();

            expect(tooltipText(context.overlayRoot)).toBe('Notifications');
        });
    });
}

function registerContentTests(): void {
    describe('content', () => {
        it('renders trusted html content when html mode is enabled', async () => {
            const context = await createContextAsync(TestHostComponent, component => {
                component.hint = '<strong>Important</strong>';
                component.html = true;
            });

            showWithMouse(context);

            expect(context.overlayRoot.querySelector('strong')?.textContent).toBe('Important');
        });

        it('renders template content with context', async () => {
            const context = await createContextAsync(TemplateHintHostComponent);
            overlayContainer = TestBed.inject(OverlayContainer);

            showWithMouse(context);

            expect(tooltipText(context.overlayRoot)).toBe('Template hint for Alex');
        });
    });
}

function registerDismissTests(): void {
    describe('dismiss', () => {
        it('cancels a pending tooltip show when the trigger is clicked', async () => {
            const context = await createContextAsync(TestHostComponent);
            overlayContainer = TestBed.inject(OverlayContainer);

            dispatchTriggerEvent(context.trigger, new MouseEvent('mouseenter', { bubbles: true }));
            context.fixture.detectChanges();

            dispatchTriggerEvent(context.trigger, new MouseEvent('click', { bubbles: true }));
            context.fixture.detectChanges();

            vi.advanceTimersByTime(HINT_SHOW_DELAY_MS);
            context.fixture.detectChanges();

            expect(queryTooltip(context.overlayRoot)).toBeNull();
        });

        it('hides visible tooltip on escape and focusout after configured delay', async () => {
            const context = await createContextAsync(TestHostComponent, component => {
                component.hideDelayMs = HINT_HIDE_DELAY_MS;
            });

            showWithMouse(context);
            dispatchTriggerEvent(context.trigger, new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
            context.fixture.detectChanges();
            expect(queryTooltip(context.overlayRoot)).toBeNull();

            showWithMouse(context);
            dispatchTriggerEvent(context.trigger, new FocusEvent('focusout', { bubbles: true }));
            context.fixture.detectChanges();
            expect(queryTooltip(context.overlayRoot)).not.toBeNull();

            vi.advanceTimersByTime(HINT_HIDE_DELAY_MS);
            context.fixture.detectChanges();
            expect(queryTooltip(context.overlayRoot)).toBeNull();
        });

        it('cleans aria state and overlay on destroy', async () => {
            const context = await createContextAsync(TestHostComponent);
            overlayContainer = TestBed.inject(OverlayContainer);

            showWithMouse(context);
            expect(queryTooltip(context.overlayRoot)).not.toBeNull();

            context.fixture.destroy();

            expect(context.trigger.getAttribute('aria-describedby')).toBeNull();
            expect(queryTooltip(context.overlayRoot)).toBeNull();
        });
    });
}
