import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';
import { Dialog } from '@angular/cdk/dialog';
import { Overlay } from '@angular/cdk/overlay';
import { Type } from '@angular/core';
import { FdUiDialogService } from './fd-ui-dialog.service';

class DummyDialogComponent {}

describe('FdUiDialogService', () => {
    it('maps detail preset to semantic panel classes', () => {
        vi.stubGlobal('window', {
            matchMedia: vi.fn().mockReturnValue({ matches: false }),
        });

        const open = vi.fn().mockReturnValue({});
        const dialogMock = { open } as unknown as Dialog;
        const overlayMock = { position: vi.fn() } as unknown as Overlay;

        TestBed.configureTestingModule({
            providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
        });

        const service = TestBed.inject(FdUiDialogService);
        service.open(DummyDialogComponent as Type<DummyDialogComponent>, { preset: 'detail' });

        expect(open).toHaveBeenCalledTimes(1);
        expect(open.mock.calls[0][1].panelClass).toContain('fd-ui-dialog-panel--detail');
        expect(open.mock.calls[0][1].panelClass).toContain('fd-ui-dialog-panel--lg');
        expect(open.mock.calls[0][1].backdropClass).toContain('fd-ui-dialog-backdrop--detail');

        vi.unstubAllGlobals();
    });

    it('maps fullscreen preset to fullscreen panel class', () => {
        vi.stubGlobal('window', {
            matchMedia: vi.fn().mockReturnValue({ matches: false }),
        });

        const open = vi.fn().mockReturnValue({});
        const dialogMock = { open } as unknown as Dialog;
        const overlayMock = { position: vi.fn() } as unknown as Overlay;

        TestBed.configureTestingModule({
            providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
        });

        const service = TestBed.inject(FdUiDialogService);
        service.open(DummyDialogComponent as Type<DummyDialogComponent>, { preset: 'fullscreen' });

        expect(open).toHaveBeenCalledTimes(1);
        expect(open.mock.calls[0][1].panelClass).toContain('fd-ui-dialog-panel--fullscreen');
        expect(open.mock.calls[0][1].panelClass).toContain('fd-ui-dialog-panel--xl');

        vi.unstubAllGlobals();
    });

    it('maps xl size to the matching panel class', () => {
        vi.stubGlobal('window', {
            matchMedia: vi.fn().mockReturnValue({ matches: false }),
        });

        const open = vi.fn().mockReturnValue({});
        const dialogMock = { open } as unknown as Dialog;
        const overlayMock = { position: vi.fn() } as unknown as Overlay;

        TestBed.configureTestingModule({
            providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
        });

        const service = TestBed.inject(FdUiDialogService);
        service.open(DummyDialogComponent as Type<DummyDialogComponent>, { size: 'xl' });

        expect(open).toHaveBeenCalledTimes(1);
        expect(open.mock.calls[0][1].panelClass).toContain('fd-ui-dialog-panel--xl');

        vi.unstubAllGlobals();
    });

    it('uses first tabbable autofocus by default', () => {
        vi.stubGlobal('window', {
            matchMedia: vi.fn().mockReturnValue({ matches: false }),
        });

        const open = vi.fn().mockReturnValue({});
        const dialogMock = { open } as unknown as Dialog;
        const overlayMock = { position: vi.fn() } as unknown as Overlay;

        TestBed.configureTestingModule({
            providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
        });

        const service = TestBed.inject(FdUiDialogService);
        service.open(DummyDialogComponent as Type<DummyDialogComponent>);

        expect(open).toHaveBeenCalledTimes(1);
        expect(open.mock.calls[0][1].autoFocus).toBe('first-tabbable');

        vi.unstubAllGlobals();
    });
});
