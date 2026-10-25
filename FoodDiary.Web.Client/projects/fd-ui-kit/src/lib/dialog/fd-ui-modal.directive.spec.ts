import { Component, inject, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiDialogService } from './fd-ui-dialog.service';
import { FdUiModalDirective } from './fd-ui-modal.directive';

@Component({ template: '<button>Confirm action</button>' })
class NestedModalComponent {}

@Component({
    imports: [FdUiModalDirective],
    template: `
        <button (click)="open.set(true)">Open panel</button>
        <input aria-label="Background field" value="1" />
        <ng-template [fdUiModal]="open()" [fdUiModalDisableClose]="busy()" fdUiModalLabel="Test panel" (fdUiModalClosed)="dismiss()">
            <input aria-label="Draft" value="Unsaved draft" />
            <button (click)="nested()">Nested</button>
        </ng-template>
    `,
})
class ModalHostComponent {
    public readonly open = signal(false);
    public readonly busy = signal(false);
    public readonly dismissed = vi.fn();
    private readonly dialogs = inject(FdUiDialogService);

    public dismiss(): void {
        this.open.set(false);
        this.dismissed();
    }

    public nested(): void {
        this.dialogs.open(NestedModalComponent);
    }
}

beforeEach(() =>
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    ),
);
afterEach(() => {
    TestBed.inject(FdUiDialogService).closeAll();
    vi.unstubAllGlobals();
});

function openPanel(): ComponentFixture<ModalHostComponent> {
    const fixture = TestBed.createComponent(ModalHostComponent);
    fixture.detectChanges();
    fixture.componentInstance.open.set(true);
    fixture.detectChanges();
    return fixture;
}

function draftInput(): HTMLInputElement {
    const input = document.querySelector<HTMLInputElement>('[role="dialog"][aria-label="Test panel"] input[aria-label="Draft"]');
    if (input === null) {
        throw new Error('Expected draft input inside the test panel');
    }
    return input;
}

function escape(element: Element | null): void {
    if (element === null) {
        throw new Error('Expected open modal');
    }
    element.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27, bubbles: true }));
}

describe('template modal lifecycle', () => {
    it('ports the template into one named dialog and closes with its owner', () => {
        const fixture = openPanel();
        const dialog = document.querySelector('[role=dialog]');
        expect(dialog?.getAttribute('aria-label')).toBe('Test panel');
        expect(document.querySelectorAll('[role=dialog]')).toHaveLength(1);
        expect(dialog?.querySelector('input')?.value).toBe('Unsaved draft');
        fixture.destroy();
        expect(document.querySelector('[role=dialog]')).toBeNull();
    });

    it('consumes Escape only at the topmost modal, preserving the underlying draft', () => {
        const fixture = openPanel();
        const input = draftInput();
        fixture.componentInstance.nested();
        fixture.detectChanges();
        escape(document.querySelectorAll('[role=dialog]').item(1));
        expect(document.querySelectorAll('[role=dialog]')).toHaveLength(1);
        expect(draftInput()).toBe(input);
        expect(input.value).toBe('Unsaved draft');
        expect(fixture.componentInstance.open()).toBe(true);
        expect(fixture.componentInstance.dismissed).not.toHaveBeenCalled();
        escape(document.querySelector('[role=dialog]'));
        expect(fixture.componentInstance.dismissed).toHaveBeenCalledOnce();
        fixture.detectChanges();
        expect(document.querySelector('[role=dialog]')).toBeNull();
    });

    it('updates the dismissal guard without recreating the panel or its draft', () => {
        const fixture = openPanel();
        const input = draftInput();
        input.value = 'Edited draft';
        fixture.componentInstance.busy.set(true);
        fixture.detectChanges();
        escape(input);
        expect(fixture.componentInstance.open()).toBe(true);
        expect(draftInput()).toBe(input);
        expect(input.value).toBe('Edited draft');
        fixture.componentInstance.busy.set(false);
        fixture.detectChanges();
        escape(input);
        expect(fixture.componentInstance.dismissed).toHaveBeenCalledOnce();
    });
});
