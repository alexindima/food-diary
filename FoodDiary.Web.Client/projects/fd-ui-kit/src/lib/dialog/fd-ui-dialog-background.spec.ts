import { Component, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiDialogService } from './fd-ui-dialog.service';

@Component({ template: '<button>Dialog action</button>' })
class BackgroundDialogComponent {}

@Component({ template: '<button (click)="open()">Open</button>' })
class BackgroundHostComponent {
    private readonly dialogs = inject(FdUiDialogService);

    protected open(): void {
        this.dialogs.open(BackgroundDialogComponent);
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

describe('modal background isolation', () => {
    it('makes page content inert until the last nested dialog closes', () => {
        const fixture = TestBed.createComponent(BackgroundHostComponent);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const service = TestBed.inject(FdUiDialogService);
        const first = service.open(BackgroundDialogComponent);
        const second = service.open(BackgroundDialogComponent);

        expect(host.hasAttribute('inert'), 'The page behind a modal must be inert').toBe(true);
        second.close();
        expect(host.hasAttribute('inert')).toBe(true);
        first.close();
        expect(host.hasAttribute('inert')).toBe(false);
    });

    it('preserves pre-existing isolation and honours a rejected close', () => {
        const fixture = TestBed.createComponent(BackgroundHostComponent);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        host.setAttribute('inert', '');
        let canClose = false;
        const dialog = TestBed.inject(FdUiDialogService).open(BackgroundDialogComponent, { closePredicate: () => canClose });
        dialog.close();
        expect(dialog.componentInstance).not.toBeNull();
        expect(host.hasAttribute('inert')).toBe(true);
        canClose = true;
        dialog.close();
        expect(host.hasAttribute('inert')).toBe(true);
    });
});
