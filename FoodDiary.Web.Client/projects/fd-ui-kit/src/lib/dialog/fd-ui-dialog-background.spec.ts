import { DOCUMENT } from '@angular/common';
import { Component, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiDialogService } from './fd-ui-dialog.service';
import { FD_UI_DIALOG_BACKGROUND } from './fd-ui-dialog-background';

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
    it('suppresses pull-to-refresh until the last dialog closes and restores the previous policy', () => {
        const style = document.documentElement.style;
        style.setProperty('overscroll-behavior-y', 'contain', 'important');
        try {
            const service = TestBed.inject(FdUiDialogService);
            const first = service.open(BackgroundDialogComponent);
            const second = service.open(BackgroundDialogComponent);
            expect(style.getPropertyValue('overscroll-behavior-y')).toBe('none');
            second.close();
            expect(style.getPropertyValue('overscroll-behavior-y')).toBe('none');
            first.close();
            expect(style.getPropertyValue('overscroll-behavior-y')).toBe('contain');
            expect(style.getPropertyPriority('overscroll-behavior-y')).toBe('important');
        } finally {
            style.removeProperty('overscroll-behavior-y');
        }
    });

    it('removes its temporary pull-to-refresh policy during lifecycle cleanup', () => {
        const style = document.documentElement.style;
        const service = TestBed.inject(FdUiDialogService);
        service.open(BackgroundDialogComponent);
        service.closeAll();
        expect(style.getPropertyValue('overscroll-behavior-y')).toBe('');
    });

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

describe('dynamic modal background content', () => {
    it('isolates new page content while preserving live announcements and the original background state', async () => {
        const alreadyIsolated = document.createElement('aside');
        alreadyIsolated.setAttribute('inert', 'owned');
        const newContent = document.createElement('main');
        const announcer = document.createElement('div');
        announcer.setAttribute('aria-live', 'polite');
        document.body.append(alreadyIsolated);

        const dialog = TestBed.inject(FdUiDialogService).open(BackgroundDialogComponent);
        try {
            document.body.append(newContent, announcer);
            await vi.waitFor(() => {
                expect(newContent.hasAttribute('inert')).toBe(true);
            });
            expect(announcer.hasAttribute('inert')).toBe(false);

            dialog.close();
            expect(newContent.hasAttribute('inert')).toBe(false);
            expect(alreadyIsolated.getAttribute('inert')).toBe('owned');
        } finally {
            dialog.close();
            alreadyIsolated.remove();
            newContent.remove();
            announcer.remove();
        }
    });

    it('isolates and restores existing content in a document without a viewport observer', () => {
        const detachedDocument = document.implementation.createHTMLDocument('Detached overlay host');
        const content = detachedDocument.createElement('main');
        detachedDocument.body.append(content);
        TestBed.configureTestingModule({ providers: [{ provide: DOCUMENT, useValue: detachedDocument }] });

        const release = TestBed.inject(FD_UI_DIALOG_BACKGROUND).acquire();
        expect(content.hasAttribute('inert')).toBe(true);
        release();
        expect(content.hasAttribute('inert')).toBe(false);
    });
});
