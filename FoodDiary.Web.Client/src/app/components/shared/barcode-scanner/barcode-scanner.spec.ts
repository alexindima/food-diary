import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { BrowserWindowService } from '../../../shared/platform/browser-window.service';
import { BarcodeScannerComponent } from './barcode-scanner';

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
        const { component, fixture } = await setupBarcodeScannerAsync({
            createBarcodeDetector: () => ({ detect: vi.fn() }),
            getUserMediaAsync: async () => camera,
            cancelAnimationFrame: vi.fn(),
            requestAnimationFrame,
        });
        fixture.detectChanges();
        if (action === 'close') {
            component['close']();
        } else {
            fixture.destroy();
        }

        resolveCamera(stream);
        await camera;
        await fixture.whenStable();

        expect(stop).toHaveBeenCalledOnce();
        expect(component['stream']).toBeNull();
        expect(requestAnimationFrame).not.toHaveBeenCalled();
    });
});
