import { OverlayContainer } from '@angular/cdk/overlay';
import { Component, inject } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { firstValueFrom, of, Subject } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { BrowserWindowService } from '../../../shared/platform/browser-window.service';
import { BarcodeScannerComponent } from './barcode-scanner';

afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
});

type BarcodeScannerTestContext = {
    component: BarcodeScannerComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<BarcodeScannerComponent>;
};

async function setupBarcodeScannerAsync(browserWindow?: Partial<BrowserWindowService>): Promise<BarcodeScannerTestContext> {
    const dialogRef = { close: vi.fn() };
    await TestBed.configureTestingModule({
        imports: [BarcodeScannerComponent],
        providers: [
            provideTranslateTesting(),
            { provide: FdUiDialogRef, useValue: dialogRef },
            ...(browserWindow === undefined ? [] : [{ provide: BrowserWindowService, useValue: browserWindow }]),
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(BarcodeScannerComponent);
    return { component: fixture.componentInstance, dialogRef, fixture };
}

describe('BarcodeScannerComponent', () => {
    it('marks scanner as unsupported when BarcodeDetector is unavailable', async () => {
        const { component, fixture } = await setupBarcodeScannerAsync();
        fixture.detectChanges();

        expect(component['isUnsupported']()).toBe(true);
    });

    it('closes dialog with null when cancelled', async () => {
        const { component, dialogRef, fixture } = await setupBarcodeScannerAsync();
        fixture.detectChanges();

        component['close']();

        expect(dialogRef.close).toHaveBeenCalledWith(null);
    });
});

describe('BarcodeScannerComponent delayed camera permission', () => {
    it.each(['close', 'destroy'] as const)('stops a stream that arrives after %s', async action => {
        let resolveCamera!: (stream: MediaStream) => void;
        const camera = new Promise<MediaStream>(resolve => {
            resolveCamera = resolve;
        });
        const stop = vi.fn();
        const stream = { getTracks: () => [{ stop }] } as unknown as MediaStream;
        const requestAnimationFrame = vi.fn();
        const getUserMediaAsync = vi.fn(async () => camera);
        const { component, fixture } = await setupBarcodeScannerAsync({
            createBarcodeDetector: () => ({ detect: vi.fn() }),
            getUserMediaAsync,
            cancelAnimationFrame: vi.fn(),
            requestAnimationFrame,
        });
        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(getUserMediaAsync).toHaveBeenCalledOnce();
        });
        if (action === 'close') {
            component['close']();
        } else {
            fixture.destroy();
        }

        resolveCamera(stream);
        await vi.waitFor(() => {
            expect(stop).toHaveBeenCalledOnce();
        });
        expect(component['stream']).toBeNull();
        expect(requestAnimationFrame).not.toHaveBeenCalled();
    });
});

describe('Barcode scanner camera setup cancellation', () => {
    it('closes through the header and releases a camera stream returned afterward', async () => {
        const camera = createCamera();
        const cameraResult = new Subject<MediaStream>();
        camera.browserWindow.getUserMediaAsync.mockImplementationOnce(async () => firstValueFrom(cameraResult));
        const play = vi.spyOn(HTMLMediaElement.prototype, 'play').mockResolvedValue(undefined);
        const { host, closed } = await openScannerAsync(camera.browserWindow);
        await vi.waitFor(() => {
            expect(cameraResult.observed).toBe(true);
        });

        closeFromHeader(host);
        cameraResult.next(camera.stream);
        cameraResult.complete();
        await vi.waitFor(() => {
            expect(camera.stop).toHaveBeenCalledOnce();
        });

        expect(play).not.toHaveBeenCalled();
        expect(camera.browserWindow.requestAnimationFrame).not.toHaveBeenCalled();
        expect(closed).toHaveBeenCalledExactlyOnceWith(undefined);
    });
});

it('requests camera access with a rendered preview before playing an immediately available stream', async () => {
    const camera = createCamera();
    const previewAtRequest: Array<Element | null> = [];
    camera.browserWindow.getUserMediaAsync.mockImplementationOnce(async () => {
        previewAtRequest.push(document.querySelector('fd-barcode-scanner video'));
        return firstValueFrom(of(camera.stream));
    });
    const play = vi.spyOn(HTMLMediaElement.prototype, 'play').mockResolvedValue(undefined);
    const { host, detectChanges } = await openScannerAsync(camera.browserWindow);
    const preview = scannerVideo(host);

    await vi.waitFor(() => {
        detectChanges();
        expect(play).toHaveBeenCalledOnce();
        expect(preview.srcObject).toBe(camera.stream);
        expect(preview.classList.contains('barcode-scanner__video--hidden')).toBe(false);
    });
    expect(previewAtRequest).toEqual([preview]);
});

describe('Barcode scanner camera errors', () => {
    it('shows a camera error when permission is denied and never starts scanning', async () => {
        const camera = createCamera();
        camera.browserWindow.getUserMediaAsync.mockRejectedValueOnce(new DOMException('Permission denied', 'NotAllowedError'));
        const { host, detectChanges, closed } = await openScannerAsync(camera.browserWindow);

        await vi.waitFor(() => {
            detectChanges();
            expect(host.textContent).toContain('BARCODE_SCANNER.CAMERA_ERROR');
        });

        expect(camera.browserWindow.requestAnimationFrame).not.toHaveBeenCalled();
        const close = host.querySelector<HTMLButtonElement>('.barcode-scanner__message button');
        expect(close).not.toBeNull();
        close?.click();
        expect(closed).toHaveBeenCalledExactlyOnceWith(null);
    });

    it('shows a camera error and releases the stream when playback is rejected', async () => {
        const camera = createCamera();
        const playback = new Subject<void>();
        const playing = firstValueFrom(playback);
        const play = vi.spyOn(HTMLMediaElement.prototype, 'play').mockReturnValue(playing);
        const { host, detectChanges, closed } = await openScannerAsync(camera.browserWindow);
        await vi.waitFor(() => {
            expect(play).toHaveBeenCalledOnce();
        });
        const rejected = expect(playing).rejects.toMatchObject({ name: 'AbortError' });
        playback.error(new DOMException('Playback interrupted', 'AbortError'));
        await rejected;

        await vi.waitFor(() => {
            detectChanges();
            expect(host.textContent).toContain('BARCODE_SCANNER.CAMERA_ERROR');
        });

        expect(host.querySelector('video')).toBeNull();
        expect(camera.stop).toHaveBeenCalledOnce();
        expect(camera.browserWindow.requestAnimationFrame).not.toHaveBeenCalled();
        expect(closed).not.toHaveBeenCalled();
    });

    it.each(['resolves', 'rejects'] as const)('keeps the scanner closed when pending playback %s', async outcome => {
        const camera = createCamera();
        const playback = new Subject<void>();
        const playing = firstValueFrom(playback);
        const play = vi.spyOn(HTMLMediaElement.prototype, 'play').mockReturnValue(playing);
        const { host, closed } = await openScannerAsync(camera.browserWindow);
        await vi.waitFor(() => {
            expect(play).toHaveBeenCalledOnce();
        });

        expect(camera.browserWindow.requestAnimationFrame).not.toHaveBeenCalled();
        closeFromHeader(host);
        if (outcome === 'resolves') {
            playback.next();
            playback.complete();
            await playing;
        } else {
            const rejected = expect(playing).rejects.toMatchObject({ name: 'AbortError' });
            playback.error(new DOMException('Playback interrupted by close', 'AbortError'));
            await rejected;
        }

        expect(closed).toHaveBeenCalledOnce();
        expect(camera.stop).toHaveBeenCalledOnce();
        expect(camera.browserWindow.requestAnimationFrame).not.toHaveBeenCalled();
    });
});

describe('Barcode scanner frame lifecycle', () => {
    it('waits for video data, retries empty and failed frames, then returns the first detected code', async () => {
        const camera = createCamera();
        vi.spyOn(HTMLMediaElement.prototype, 'play').mockResolvedValue(undefined);
        const { host, detectChanges, closed } = await openScannerAsync(camera.browserWindow);
        const video = scannerVideo(host);
        camera.detect
            .mockResolvedValueOnce([])
            .mockRejectedValueOnce(new Error('Video frame unavailable'))
            .mockResolvedValueOnce([BARCODE, { ...BARCODE, rawValue: '8801043014750' }]);

        await vi.waitFor(() => {
            detectChanges();
            expect(video.classList.contains('barcode-scanner__video--hidden')).toBe(false);
        });
        expect(video.srcObject).toBe(camera.stream);
        expect(camera.browserWindow.getUserMediaAsync).toHaveBeenCalledExactlyOnceWith({ video: { facingMode: 'environment' } });

        camera.deliverFrame();
        expect(camera.detect).not.toHaveBeenCalled();
        Object.defineProperty(video, 'readyState', { value: video.HAVE_ENOUGH_DATA });
        for (const skipped of ['empty', 'failed']) {
            camera.deliverFrame();
            await vi.waitFor(() => {
                expect(camera.hasPendingFrame(), `No scan requested after a ${skipped} frame`).toBe(true);
            });
            expect(closed).not.toHaveBeenCalled();
        }
        camera.deliverFrame();

        await vi.waitFor(() => {
            expect(closed).toHaveBeenCalledExactlyOnceWith(BARCODE.rawValue);
        });
        expect(camera.detect).toHaveBeenLastCalledWith(video);
        expect(camera.stop).toHaveBeenCalledOnce();
        expect(camera.hasPendingFrame()).toBe(false);
    });

    it.each(['resolves', 'rejects'] as const)('keeps the camera released when a closed dialog detection %s', async outcome => {
        const camera = createCamera();
        vi.spyOn(HTMLMediaElement.prototype, 'play').mockResolvedValue(undefined);
        const frameResult = new Subject<DetectedBarcode[]>();
        const detection = firstValueFrom(frameResult);
        camera.detect.mockReturnValueOnce(detection);
        const { host, closed } = await openScannerAsync(camera.browserWindow);
        const video = scannerVideo(host);
        Object.defineProperty(video, 'readyState', { value: video.HAVE_ENOUGH_DATA });
        await vi.waitFor(() => {
            expect(camera.hasPendingFrame()).toBe(true);
        });
        camera.deliverFrame();
        expect(camera.detect).toHaveBeenCalledExactlyOnceWith(video);

        closeFromHeader(host);
        if (outcome === 'resolves') {
            frameResult.next([BARCODE]);
            frameResult.complete();
            await detection;
        } else {
            const rejected = expect(detection).rejects.toThrow('Frame no longer available');
            frameResult.error(new Error('Frame no longer available'));
            await rejected;
        }

        expect(camera.browserWindow.requestAnimationFrame).toHaveBeenCalledOnce();
        expect(closed).toHaveBeenCalledOnce();
        expect(camera.stop).toHaveBeenCalledOnce();
        expect(camera.hasPendingFrame()).toBe(false);
    });
});

const ESCAPE_KEY_CODE = 27;
const REOPEN_REQUEST_COUNT = 2;

@Component({
    template: '<button type="button" (click)="openScanner()">Scan barcode</button>',
})
class BarcodeScannerLauncher {
    private readonly dialogs = inject(FdUiDialogService);
    private dialog: FdUiDialogRef<BarcodeScannerComponent, string | null> | null = null;
    public readonly results = vi.fn<(code: string | null | undefined) => void>();

    public detectScannerChanges(): void {
        const component = this.dialog?.componentRef;
        if (component === null || component === undefined) {
            throw new Error('Expected the scanner dialog component');
        }
        component.changeDetectorRef.detectChanges();
    }

    protected openScanner(): void {
        const dialog = this.dialogs.open<BarcodeScannerComponent, null, string | null>(BarcodeScannerComponent, { size: 'lg' });
        this.dialog = dialog;
        dialog.afterClosed().subscribe(this.results);
    }
}

it('restores focus on Escape and keeps the reopened scanner independent of late camera permission', async () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    vi.spyOn(HTMLMediaElement.prototype, 'play').mockResolvedValue(undefined);
    const oldPermission = new Subject<MediaStream>();
    const oldCamera = createCamera();
    const currentCamera = createCamera();
    currentCamera.browserWindow.getUserMediaAsync.mockReturnValueOnce(firstValueFrom(oldPermission));
    currentCamera.detect.mockResolvedValueOnce([BARCODE]);

    await TestBed.configureTestingModule({
        imports: [BarcodeScannerLauncher, BarcodeScannerComponent],
        providers: [provideTranslateTesting()],
    })
        .overrideComponent(BarcodeScannerComponent, {
            add: { providers: [{ provide: BrowserWindowService, useValue: currentCamera.browserWindow }] },
        })
        .compileComponents();
    const fixture = TestBed.createComponent(BarcodeScannerLauncher);
    fixture.detectChanges();
    const launcher = (fixture.nativeElement as HTMLElement).querySelector('button');
    if (launcher === null) {
        throw new Error('Expected the scanner launcher');
    }

    const overlay = TestBed.inject(OverlayContainer).getContainerElement();
    launcher.focus();
    launcher.click();
    fixture.componentInstance.detectScannerChanges();
    const firstScanner = requireScannerHost(overlay);
    await vi.waitFor(() => {
        fixture.componentInstance.detectScannerChanges();
        expect(currentCamera.browserWindow.getUserMediaAsync).toHaveBeenCalledOnce();
    });
    const close = scannerCloseButton(firstScanner);
    close.focus();
    expect(document.activeElement).toBe(close);
    close.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: ESCAPE_KEY_CODE, bubbles: true, cancelable: true }));
    expect(firstScanner.isConnected).toBe(false);
    expect(document.activeElement).toBe(launcher);
    expect(fixture.componentInstance.results).toHaveBeenCalledExactlyOnceWith(undefined);

    launcher.click();
    fixture.componentInstance.detectScannerChanges();
    const liveScanner = requireScannerHost(overlay);
    const video = scannerVideo(liveScanner);
    await vi.waitFor(() => {
        fixture.componentInstance.detectScannerChanges();
        expect(video.srcObject).toBe(currentCamera.stream);
        expect(video.classList.contains('barcode-scanner__video--hidden')).toBe(false);
    });
    expect(currentCamera.browserWindow.getUserMediaAsync).toHaveBeenCalledTimes(REOPEN_REQUEST_COUNT);

    oldPermission.next(oldCamera.stream);
    oldPermission.complete();
    await vi.waitFor(() => {
        expect(oldCamera.stop).toHaveBeenCalledOnce();
    });
    expect(liveScanner.isConnected).toBe(true);
    expect(video.srcObject).toBe(currentCamera.stream);
    expect(currentCamera.stop).not.toHaveBeenCalled();
    expect(fixture.componentInstance.results).toHaveBeenCalledOnce();

    Object.defineProperty(video, 'readyState', { value: video.HAVE_ENOUGH_DATA });
    currentCamera.deliverFrame();
    await vi.waitFor(() => {
        expect(fixture.componentInstance.results.mock.calls).toEqual([[undefined], [BARCODE.rawValue]]);
    });
    expect(currentCamera.detect).toHaveBeenCalledExactlyOnceWith(video);
    expect(currentCamera.stop).toHaveBeenCalledOnce();
    expect(oldCamera.stop).toHaveBeenCalledOnce();
    expect(liveScanner.isConnected).toBe(false);
    expect(document.activeElement).toBe(launcher);
});

function requireScannerHost(overlay: HTMLElement): HTMLElement {
    const scanner = overlay.querySelector<HTMLElement>('fd-barcode-scanner');
    if (scanner === null) {
        throw new Error('Expected an open scanner dialog');
    }
    return scanner;
}

const BARCODE: DetectedBarcode = {
    rawValue: '5901234123457',
    format: 'ean_13',
    boundingBox: new DOMRectReadOnly(),
    cornerPoints: [],
};

type ScannerCameraBoundary = Pick<
    BrowserWindowService,
    'createBarcodeDetector' | 'getUserMediaAsync' | 'requestAnimationFrame' | 'cancelAnimationFrame'
>;

async function openScannerAsync(browserWindow: ScannerCameraBoundary): Promise<{
    host: HTMLElement;
    detectChanges: () => void;
    closed: ReturnType<typeof vi.fn<(code: string | null | undefined) => void>>;
}> {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    await TestBed.configureTestingModule({
        imports: [BarcodeScannerComponent],
        providers: [provideTranslateTesting(), { provide: BrowserWindowService, useValue: browserWindow }],
    }).compileComponents();
    const dialog = TestBed.inject(FdUiDialogService).open<BarcodeScannerComponent, null, string | null>(BarcodeScannerComponent);
    const component = dialog.componentRef;
    if (component === null) {
        throw new Error('Scanner dialog did not create its component');
    }
    const closed = vi.fn<(code: string | null | undefined) => void>();
    dialog.afterClosed().subscribe(closed);
    const detectChanges = (): void => {
        component.changeDetectorRef.detectChanges();
    };
    detectChanges();
    return { host: component.location.nativeElement as HTMLElement, detectChanges, closed };
}

function scannerVideo(host: HTMLElement): HTMLVideoElement {
    const video = host.querySelector('video');
    if (video === null) {
        throw new Error('Scanner preview is missing');
    }
    return video;
}

function closeFromHeader(host: HTMLElement): void {
    scannerCloseButton(host).click();
}

function scannerCloseButton(host: HTMLElement): HTMLButtonElement {
    const close = host.querySelector<HTMLButtonElement>('.fd-ui-dialog__close-button');
    if (close === null) {
        throw new Error('Scanner close control is missing');
    }
    return close;
}

function createCamera(): {
    browserWindow: { [Key in keyof ScannerCameraBoundary]: ReturnType<typeof vi.fn<ScannerCameraBoundary[Key]>> };
    detect: ReturnType<typeof vi.fn<BarcodeDetector['detect']>>;
    deliverFrame: () => void;
    hasPendingFrame: () => boolean;
    stop: ReturnType<typeof vi.fn>;
    stream: MediaStream;
} {
    const frames = new Map<number, FrameRequestCallback>();
    let lastFrameId = 0;
    const stop = vi.fn();
    const stream = { getTracks: () => [{ stop }] } as unknown as MediaStream;
    const detect = vi.fn<BarcodeDetector['detect']>();
    const browserWindow = {
        createBarcodeDetector: vi.fn<BrowserWindowService['createBarcodeDetector']>().mockReturnValue({ detect }),
        getUserMediaAsync: vi.fn<BrowserWindowService['getUserMediaAsync']>().mockResolvedValue(stream),
        requestAnimationFrame: vi.fn<BrowserWindowService['requestAnimationFrame']>(callback => {
            const id = ++lastFrameId;
            frames.set(id, callback);
            return id;
        }),
        cancelAnimationFrame: vi.fn<BrowserWindowService['cancelAnimationFrame']>(id => {
            if (id !== null) {
                frames.delete(id);
            }
        }),
    };
    const deliverFrame = (): void => {
        const frame = frames.entries().next().value;
        if (frame === undefined) {
            throw new Error('No pending scanner frame');
        }
        const [id, callback] = frame;
        frames.delete(id);
        callback(0);
    };
    return { browserWindow, detect, deliverFrame, hasPendingFrame: (): boolean => frames.size > 0, stop, stream };
}
