import { Dialog } from '@angular/cdk/dialog';
import { Overlay } from '@angular/cdk/overlay';
import { type Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { FdUiDialogService } from './fd-ui-dialog.service';

class DummyDialogComponent {}

function createOverlayMock(): {
    overlayMock: Overlay;
    strategy: {
        centerHorizontally: ReturnType<typeof vi.fn>;
        top: ReturnType<typeof vi.fn>;
        bottom: ReturnType<typeof vi.fn>;
    };
} {
    const strategy = {
        centerHorizontally: vi.fn(),
        top: vi.fn(),
        bottom: vi.fn(),
    };
    strategy.centerHorizontally.mockReturnValue(strategy);
    strategy.top.mockReturnValue(strategy);
    strategy.bottom.mockReturnValue(strategy);

    return {
        overlayMock: {
            position: vi.fn().mockReturnValue({
                global: vi.fn().mockReturnValue(strategy),
            }),
        } as unknown as Overlay,
        strategy,
    };
}

describe('FdUiDialogService', () => {
    it('maps detail preset to semantic panel classes', () => {
        vi.stubGlobal('window', {
            matchMedia: vi.fn().mockReturnValue({ matches: false }),
        });

        const open = vi.fn().mockReturnValue({});
        const dialogMock = { open } as unknown as Dialog;
        const { overlayMock } = createOverlayMock();

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
        const { overlayMock } = createOverlayMock();

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
        const { overlayMock } = createOverlayMock();

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
        const { overlayMock } = createOverlayMock();

        TestBed.configureTestingModule({
            providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
        });

        const service = TestBed.inject(FdUiDialogService);
        service.open(DummyDialogComponent as Type<DummyDialogComponent>);

        expect(open).toHaveBeenCalledTimes(1);
        expect(open.mock.calls[0][1].autoFocus).toBe('first-tabbable');

        vi.unstubAllGlobals();
    });

    it('top-aligns regular dialogs to keep tabbed headers stable', () => {
        vi.stubGlobal('window', {
            matchMedia: vi.fn().mockReturnValue({ matches: false }),
        });

        const open = vi.fn().mockReturnValue({});
        const dialogMock = { open } as unknown as Dialog;
        const { overlayMock, strategy } = createOverlayMock();

        TestBed.configureTestingModule({
            providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
        });

        const service = TestBed.inject(FdUiDialogService);
        service.open(DummyDialogComponent as Type<DummyDialogComponent>);

        expect(strategy.centerHorizontally).toHaveBeenCalledTimes(1);
        expect(strategy.top).toHaveBeenCalledWith('40px');
        expect(strategy.bottom).not.toHaveBeenCalled();
        expect(open.mock.calls[0][1].positionStrategy).toBe(strategy);

        vi.unstubAllGlobals();
    });

    it('keeps compact mobile dialogs anchored to the bottom edge', () => {
        vi.stubGlobal('window', {
            matchMedia: vi.fn().mockReturnValue({ matches: true }),
        });

        const open = vi.fn().mockReturnValue({});
        const dialogMock = { open } as unknown as Dialog;
        const { overlayMock, strategy } = createOverlayMock();

        TestBed.configureTestingModule({
            providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
        });

        const service = TestBed.inject(FdUiDialogService);
        service.open(DummyDialogComponent as Type<DummyDialogComponent>);

        expect(strategy.centerHorizontally).toHaveBeenCalledTimes(1);
        expect(strategy.bottom).toHaveBeenCalledWith('0');
        expect(strategy.top).not.toHaveBeenCalled();
        expect(open.mock.calls[0][1].positionStrategy).toBe(strategy);

        vi.unstubAllGlobals();
    });
});
