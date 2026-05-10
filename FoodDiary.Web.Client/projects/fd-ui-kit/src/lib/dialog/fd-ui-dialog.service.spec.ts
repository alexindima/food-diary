import { Dialog } from '@angular/cdk/dialog';
import { type GlobalPositionStrategy, Overlay } from '@angular/cdk/overlay';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { FdUiDialogService } from './fd-ui-dialog.service';

class DummyDialogComponent {}

type DialogOpenMock = ReturnType<typeof vi.fn<Dialog['open']>>;
type DialogOpenConfig = NonNullable<Parameters<Dialog['open']>[1]>;
type StrategyMock = {
    centerHorizontally: ReturnType<typeof vi.fn<() => GlobalPositionStrategy>>;
    top: ReturnType<typeof vi.fn<(value: string) => GlobalPositionStrategy>>;
    bottom: ReturnType<typeof vi.fn<(value: string) => GlobalPositionStrategy>>;
};

function createOverlayMock(): {
    overlayMock: Overlay;
    strategy: StrategyMock;
} {
    const strategy: StrategyMock = {
        centerHorizontally: vi.fn<() => GlobalPositionStrategy>(),
        top: vi.fn<(value: string) => GlobalPositionStrategy>(),
        bottom: vi.fn<(value: string) => GlobalPositionStrategy>(),
    };
    const globalStrategy = strategy as unknown as GlobalPositionStrategy;
    strategy.centerHorizontally.mockReturnValue(globalStrategy);
    strategy.top.mockReturnValue(globalStrategy);
    strategy.bottom.mockReturnValue(globalStrategy);

    const overlayMock: Overlay = {
        position: vi.fn().mockReturnValue({
            global: vi.fn().mockReturnValue(strategy),
        }),
    } as unknown as Overlay;

    return {
        overlayMock,
        strategy,
    };
}

function createDialogOpenMock(): DialogOpenMock {
    return vi.fn<Dialog['open']>();
}

function createDialogMock(open: DialogOpenMock): Dialog {
    const dialogMock: Dialog = { open } as unknown as Dialog;
    return dialogMock;
}

function latestConfig(open: DialogOpenMock): DialogOpenConfig {
    const config = open.mock.calls[0]?.[1];
    if (config === undefined) {
        throw new Error('Expected dialog open config to exist.');
    }

    return config;
}

describe('FdUiDialogService', () => {
    it('maps detail preset to semantic panel classes', () => {
        vi.stubGlobal('window', {
            matchMedia: vi.fn().mockReturnValue({ matches: false }),
        });

        const open = createDialogOpenMock();
        const dialogMock = createDialogMock(open);
        const { overlayMock } = createOverlayMock();

        TestBed.configureTestingModule({
            providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
        });

        const service = TestBed.inject(FdUiDialogService);
        service.open(DummyDialogComponent, { preset: 'detail' });

        expect(open).toHaveBeenCalledTimes(1);
        expect(latestConfig(open).panelClass).toContain('fd-ui-dialog-panel--detail');
        expect(latestConfig(open).panelClass).toContain('fd-ui-dialog-panel--lg');
        expect(latestConfig(open).backdropClass).toContain('fd-ui-dialog-backdrop--detail');

        vi.unstubAllGlobals();
    });

    it('maps fullscreen preset to fullscreen panel class', () => {
        vi.stubGlobal('window', {
            matchMedia: vi.fn().mockReturnValue({ matches: false }),
        });

        const open = createDialogOpenMock();
        const dialogMock = createDialogMock(open);
        const { overlayMock } = createOverlayMock();

        TestBed.configureTestingModule({
            providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
        });

        const service = TestBed.inject(FdUiDialogService);
        service.open(DummyDialogComponent, { preset: 'fullscreen' });

        expect(open).toHaveBeenCalledTimes(1);
        expect(latestConfig(open).panelClass).toContain('fd-ui-dialog-panel--fullscreen');
        expect(latestConfig(open).panelClass).toContain('fd-ui-dialog-panel--xl');

        vi.unstubAllGlobals();
    });

    it('maps xl size to the matching panel class', () => {
        vi.stubGlobal('window', {
            matchMedia: vi.fn().mockReturnValue({ matches: false }),
        });

        const open = createDialogOpenMock();
        const dialogMock = createDialogMock(open);
        const { overlayMock } = createOverlayMock();

        TestBed.configureTestingModule({
            providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
        });

        const service = TestBed.inject(FdUiDialogService);
        service.open(DummyDialogComponent, { size: 'xl' });

        expect(open).toHaveBeenCalledTimes(1);
        expect(latestConfig(open).panelClass).toContain('fd-ui-dialog-panel--xl');

        vi.unstubAllGlobals();
    });

    it('uses first tabbable autofocus by default', () => {
        vi.stubGlobal('window', {
            matchMedia: vi.fn().mockReturnValue({ matches: false }),
        });

        const open = createDialogOpenMock();
        const dialogMock = createDialogMock(open);
        const { overlayMock } = createOverlayMock();

        TestBed.configureTestingModule({
            providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
        });

        const service = TestBed.inject(FdUiDialogService);
        service.open(DummyDialogComponent);

        expect(open).toHaveBeenCalledTimes(1);
        expect(latestConfig(open).autoFocus).toBe('first-tabbable');

        vi.unstubAllGlobals();
    });

    it('top-aligns regular dialogs to keep tabbed headers stable', () => {
        vi.stubGlobal('window', {
            matchMedia: vi.fn().mockReturnValue({ matches: false }),
        });

        const open = createDialogOpenMock();
        const dialogMock = createDialogMock(open);
        const { overlayMock, strategy } = createOverlayMock();

        TestBed.configureTestingModule({
            providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
        });

        const service = TestBed.inject(FdUiDialogService);
        service.open(DummyDialogComponent);

        expect(strategy.centerHorizontally).toHaveBeenCalledTimes(1);
        expect(strategy.top).toHaveBeenCalledWith('40px');
        expect(strategy.bottom).not.toHaveBeenCalled();
        expect(latestConfig(open).positionStrategy).toBe(strategy);

        vi.unstubAllGlobals();
    });

    it('keeps compact mobile dialogs anchored to the bottom edge', () => {
        vi.stubGlobal('window', {
            matchMedia: vi.fn().mockReturnValue({ matches: true }),
        });

        const open = createDialogOpenMock();
        const dialogMock = createDialogMock(open);
        const { overlayMock, strategy } = createOverlayMock();

        TestBed.configureTestingModule({
            providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
        });

        const service = TestBed.inject(FdUiDialogService);
        service.open(DummyDialogComponent);

        expect(strategy.centerHorizontally).toHaveBeenCalledTimes(1);
        expect(strategy.bottom).toHaveBeenCalledWith('0');
        expect(strategy.top).not.toHaveBeenCalled();
        expect(latestConfig(open).positionStrategy).toBe(strategy);

        vi.unstubAllGlobals();
    });
});
