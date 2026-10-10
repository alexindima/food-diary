import { DOCUMENT } from '@angular/common';
import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiInputComponent } from '../input/fd-ui-input';
import { FdUiTextareaComponent } from '../textarea/fd-ui-textarea';
import { FdUiActivateDirective } from './fd-ui-activate.directive';
import { FdUiFocusWhenDirective } from './fd-ui-focus-when.directive';
import { FdUiPreventUnloadDirective } from './fd-ui-prevent-unload.directive';
import { FdUiStopActivationDirective } from './fd-ui-stop-activation.directive';

const EXPECTED_ACTIVATION_COUNT = 3;

@Component({
    imports: [
        FdUiActivateDirective,
        FdUiStopActivationDirective,
        FdUiFocusWhenDirective,
        FdUiPreventUnloadDirective,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiTextareaComponent,
    ],
    template: `
        <div id="card" fdUiActivate [fdUiActivateDisabled]="disabled()" (fdUiActivated)="activated.update(increment)">
            <span id="label">Card</span>
            <button id="native" type="button" (click)="nested.update(increment)">Nested</button>
            <input id="nested-input" aria-label="Nested input" />
        </div>
        <table>
            <tbody>
                <tr id="row" fdUiActivate [fdUiActivateRole]="null">
                    <td>Row</td>
                </tr>
            </tbody>
        </table>
        <div (click)="parentClicks.update(increment)" (keydown)="parentKeys.update(increment)">
            <button id="isolated" type="button" fdUiStopActivation (click)="nested.update(increment)">Action</button>
            <button id="keyboard-only" type="button" fdUiStopActivation fdUiStopActivationKeyboardOnly>Overlay action</button>
        </div>
        <fd-ui-button
            id="focus-button"
            [fdUiFocusWhen]="focus()"
            [disabled]="busy()"
            [loading]="loading()"
            (fdUiFocused)="focusCount.update(increment)"
            >Focus</fd-ui-button
        >
        <fd-ui-input id="focus-input" label="Name" [fdUiFocusWhen]="inputFocus()" />
        <fieldset [disabled]="fieldsetLocked()">
            <fd-ui-input
                class="fieldset-input"
                label="Locked name"
                [fdUiFocusWhen]="fieldsetFocus()"
                (fdUiFocused)="fieldsetCount.update(increment)"
            />
        </fieldset>
        <fd-ui-textarea id="focus-textarea" label="Description" [disabled]="busy()" [fdUiFocusWhen]="textareaFocus()" />
        <input id="select-input" aria-label="Rename" [fdUiFocusWhen]="selectFocus()" fdUiFocusSelect [value]="draft()" />
        <div [fdUiPreventUnload]="dirty()"></div>
    `,
})
class InteractionHostComponent {
    public readonly disabled = signal(false);
    public readonly activated = signal(0);
    public readonly nested = signal(0);
    public readonly parentClicks = signal(0);
    public readonly parentKeys = signal(0);
    public readonly focus = signal(false);
    public readonly inputFocus = signal(false);
    public readonly fieldsetLocked = signal(true);
    public readonly fieldsetFocus = signal(false);
    public readonly fieldsetCount = signal(0);
    public readonly textareaFocus = signal(false);
    public readonly selectFocus = signal(false);
    public readonly busy = signal(false);
    public readonly loading = signal(false);
    public readonly focusCount = signal(0);
    public readonly draft = signal('Shopping list');
    public readonly dirty = signal(false);
    public readonly increment = (value: number): number => value + 1;
}

async function setupAsync(): Promise<ComponentFixture<InteractionHostComponent>> {
    const fixture = TestBed.createComponent(InteractionHostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
}

function element(fixture: ComponentFixture<InteractionHostComponent>, selector: string): HTMLElement {
    const found = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>(selector);
    if (found === null) {
        throw new Error(`Missing ${selector}`);
    }
    return found;
}

describe('activation and nested actions', () => {
    it('activates from a label click, Enter and Space, preventing keyboard defaults', async () => {
        const fixture = await setupAsync();
        element(fixture, '#label').click();
        for (const key of ['Enter', ' ']) {
            const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
            element(fixture, '#card').dispatchEvent(event);
            expect(event.defaultPrevented).toBe(true);
        }
        expect(fixture.componentInstance.activated()).toBe(EXPECTED_ACTIVATION_COUNT);
        expect(element(fixture, '#card').getAttribute('role')).toBe('button');
        expect(element(fixture, '#row').hasAttribute('role')).toBe(false);
    });

    it('does not activate the parent from nested native controls or their keyboard events', async () => {
        const fixture = await setupAsync();
        element(fixture, '#native').click();
        for (const selector of ['#native', '#nested-input']) {
            element(fixture, selector).dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        }
        expect(fixture.componentInstance.nested()).toBe(1);
        expect(fixture.componentInstance.activated()).toBe(0);
    });

    it('ignores disabled, modified and repeated activations', async () => {
        const fixture = await setupAsync();
        const card = element(fixture, '#card');
        card.dispatchEvent(new MouseEvent('click', { button: 1 }));
        card.dispatchEvent(new MouseEvent('click', { ctrlKey: true }));
        card.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', repeat: true }));
        fixture.componentInstance.disabled.set(true);
        fixture.detectChanges();
        card.click();
        card.dispatchEvent(new KeyboardEvent('keydown', { key: ' ' }));
        expect(fixture.componentInstance.activated()).toBe(0);
        expect(card.getAttribute('tabindex')).toBe('-1');
        expect(card.getAttribute('aria-disabled')).toBe('true');
    });

    it('isolates nested actions while preserving native keyboard defaults and optional click bubbling', async () => {
        const fixture = await setupAsync();
        const isolated = element(fixture, '#isolated');
        const event = new KeyboardEvent('keydown', { key: ' ', bubbles: true, cancelable: true });
        isolated.dispatchEvent(event);
        isolated.click();
        expect(event.defaultPrevented).toBe(false);
        expect(fixture.componentInstance.parentKeys()).toBe(0);
        expect(fixture.componentInstance.parentClicks()).toBe(0);
        expect(fixture.componentInstance.nested()).toBe(1);
        element(fixture, '#keyboard-only').click();
        expect(fixture.componentInstance.parentClicks()).toBe(1);
    });
});

describe('focus requests', () => {
    it('does not acknowledge blocked focus and retries when an ancestor fieldset is enabled', async () => {
        const fixture = await setupAsync();
        const host = fixture.componentInstance;
        const control = element(fixture, '.fieldset-input input');
        host.fieldsetFocus.set(true);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(host.fieldsetCount()).toBe(0);
        expect(TestBed.inject(DOCUMENT).activeElement).not.toBe(control);
        host.fieldsetLocked.set(false);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(TestBed.inject(DOCUMENT).activeElement).toBe(control);
        expect(host.fieldsetCount()).toBe(1);
    });
    it('waits for an enabled UI-kit textarea and focuses its native control', async () => {
        const fixture = await setupAsync();
        fixture.componentInstance.busy.set(true);
        fixture.componentInstance.textareaFocus.set(true);
        fixture.detectChanges();
        await fixture.whenStable();
        const textarea = element(fixture, 'fd-ui-textarea textarea');
        expect(TestBed.inject(DOCUMENT).activeElement).not.toBe(textarea);
        fixture.componentInstance.busy.set(false);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(TestBed.inject(DOCUMENT).activeElement).toBe(textarea);
    });
    it('waits for an enabled button and focuses only once per request', async () => {
        const fixture = await setupAsync();
        const host = fixture.componentInstance;
        host.busy.set(true);
        host.focus.set(true);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(host.focusCount()).toBe(0);
        host.busy.set(false);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(TestBed.inject(DOCUMENT).activeElement).toBe(element(fixture, '#focus-button button'));
        expect(host.focusCount()).toBe(1);
        element(fixture, '#nested-input').focus();
        host.draft.set('Changed elsewhere');
        fixture.detectChanges();
        await fixture.whenStable();
        expect(TestBed.inject(DOCUMENT).activeElement).toBe(element(fixture, '#nested-input'));
        host.focus.set(false);
        fixture.detectChanges();
        await fixture.whenStable();
        host.focus.set(true);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(host.focusCount()).toBe(2);
    });

    it('focuses the UI-kit input through its public contract and selects a native rename draft', async () => {
        const fixture = await setupAsync();
        fixture.componentInstance.inputFocus.set(true);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(TestBed.inject(DOCUMENT).activeElement).toBe(element(fixture, 'fd-ui-input input'));
        fixture.componentInstance.selectFocus.set(true);
        fixture.detectChanges();
        await fixture.whenStable();
        const input = element(fixture, '#select-input') as HTMLInputElement;
        expect(TestBed.inject(DOCUMENT).activeElement).toBe(input);
        expect(input.selectionStart).toBe(0);
        expect(input.selectionEnd).toBe(input.value.length);
    });
});

describe('unload protection', () => {
    it('protects only dirty owners and releases the listener after save and destruction', async () => {
        const fixture = await setupAsync();
        const browserWindow = TestBed.inject(DOCUMENT).defaultView;
        if (browserWindow === null) {
            throw new Error('Expected browser document');
        }
        const prevented = (): boolean => {
            const event = new Event('beforeunload', { cancelable: true });
            browserWindow.dispatchEvent(event);
            return event.defaultPrevented;
        };
        expect(prevented()).toBe(false);
        fixture.componentInstance.dirty.set(true);
        fixture.detectChanges();
        expect(prevented()).toBe(true);
        fixture.componentInstance.dirty.set(false);
        fixture.detectChanges();
        expect(prevented()).toBe(false);
        fixture.componentInstance.dirty.set(true);
        fixture.detectChanges();
        fixture.destroy();
        expect(prevented()).toBe(false);
    });
});
