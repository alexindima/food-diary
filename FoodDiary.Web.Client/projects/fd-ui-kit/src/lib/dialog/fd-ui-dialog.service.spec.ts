import { Dialog } from '@angular/cdk/dialog';
import { type GlobalPositionStrategy, Overlay } from '@angular/cdk/overlay';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { FdUiDialogService } from './fd-ui-dialog.service';

@Component({
    template: '',
})
class DummyDialogComponent {}

type DialogOpenMock = ReturnType<typeof vi.fn<Dialog['open']>>;
type DialogOpenConfig = NonNullable<Parameters<Dialog['open']>[1]>;
type StrategyMock = {
    centerHorizontally: ReturnType<typeof vi.fn<() => GlobalPositionStrategy>>;
    top: ReturnType<typeof vi.fn<(value: string) => GlobalPositionStrategy>>;
    bottom: ReturnType<typeof vi.fn<(value: string) => GlobalPositionStrategy>>;
};
type DialogServiceTestContext = {
    open: DialogOpenMock;
    service: FdUiDialogService;
    strategy: StrategyMock;
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

function setupDialogService(matches: boolean): DialogServiceTestContext {
    vi.stubGlobal('window', {
        matchMedia: vi.fn().mockReturnValue({ matches }),
    });

    const open = createDialogOpenMock();
    const dialogMock = createDialogMock(open);
    const { overlayMock, strategy } = createOverlayMock();

    TestBed.configureTestingModule({
        providers: [FdUiDialogService, { provide: Dialog, useValue: dialogMock }, { provide: Overlay, useValue: overlayMock }],
    });

    const service = TestBed.inject(FdUiDialogService);

    return { open, service, strategy };
}

afterEach(() => {
    vi.unstubAllGlobals();
});

describe('FdUiDialogService panel classes', () => {
    it('maps detail preset to semantic panel classes', () => {
        const { open, service } = setupDialogService(false);
        service.open(DummyDialogComponent, { preset: 'detail' });

        expect(open).toHaveBeenCalledTimes(1);
        expect(latestConfig(open).panelClass).toContain('fd-ui-dialog-panel--detail');
        expect(latestConfig(open).panelClass).toContain('fd-ui-dialog-panel--lg');
        expect(latestConfig(open).backdropClass).toContain('fd-ui-dialog-backdrop--detail');
    });

    it('maps fullscreen preset to fullscreen panel class', () => {
        const { open, service } = setupDialogService(false);
        service.open(DummyDialogComponent, { preset: 'fullscreen' });

        expect(open).toHaveBeenCalledTimes(1);
        expect(latestConfig(open).panelClass).toContain('fd-ui-dialog-panel--fullscreen');
        expect(latestConfig(open).panelClass).toContain('fd-ui-dialog-panel--xl');
    });

    it('maps xl size to the matching panel class', () => {
        const { open, service } = setupDialogService(false);
        service.open(DummyDialogComponent, { size: 'xl' });

        expect(open).toHaveBeenCalledTimes(1);
        expect(latestConfig(open).panelClass).toContain('fd-ui-dialog-panel--xl');
    });
});

describe('FdUiDialogService options', () => {
    it('uses first tabbable autofocus by default', () => {
        const { open, service } = setupDialogService(false);
        service.open(DummyDialogComponent);

        expect(open).toHaveBeenCalledTimes(1);
        expect(latestConfig(open).autoFocus).toBe('first-tabbable');
    });
});

describe('FdUiDialogService positioning', () => {
    it('top-aligns regular dialogs to keep tabbed headers stable', () => {
        const { open, service, strategy } = setupDialogService(false);
        service.open(DummyDialogComponent);

        expect(strategy.centerHorizontally).toHaveBeenCalledTimes(1);
        expect(strategy.top).toHaveBeenCalledWith('40px');
        expect(strategy.bottom).not.toHaveBeenCalled();
        expect(latestConfig(open).positionStrategy).toBe(strategy);
    });

    it('keeps compact mobile dialogs anchored to the bottom edge', () => {
        const { open, service, strategy } = setupDialogService(true);
        service.open(DummyDialogComponent);

        expect(strategy.centerHorizontally).toHaveBeenCalledTimes(1);
        expect(strategy.bottom).toHaveBeenCalledWith('0');
        expect(strategy.top).not.toHaveBeenCalled();
        expect(latestConfig(open).positionStrategy).toBe(strategy);
    });
});
