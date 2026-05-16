import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { BarcodeScannerComponent } from './barcode-scanner.component';

type BarcodeScannerTestContext = {
    component: BarcodeScannerComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<BarcodeScannerComponent>;
};

async function setupBarcodeScannerAsync(): Promise<BarcodeScannerTestContext> {
    const dialogRef = { close: vi.fn() };
    await TestBed.configureTestingModule({
        imports: [BarcodeScannerComponent, TranslateModule.forRoot()],
        providers: [{ provide: FdUiDialogRef, useValue: dialogRef }],
    }).compileComponents();

    const fixture = TestBed.createComponent(BarcodeScannerComponent);
    return { component: fixture.componentInstance, dialogRef, fixture };
}

describe('BarcodeScannerComponent', () => {
    it('marks scanner as unsupported when BarcodeDetector is unavailable', async () => {
        const { component, fixture } = await setupBarcodeScannerAsync();
        fixture.detectChanges();

        expect(component.isUnsupported()).toBe(true);
    });

    it('closes dialog with null when cancelled', async () => {
        const { component, dialogRef, fixture } = await setupBarcodeScannerAsync();
        fixture.detectChanges();

        component.close();

        expect(dialogRef.close).toHaveBeenCalledWith(null);
    });
});
