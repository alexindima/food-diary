import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { AiConsentDialogComponent } from './ai-consent-dialog';

type AiConsentDialogTestContext = {
    component: AiConsentDialogComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<AiConsentDialogComponent>;
};

async function setupAiConsentDialogAsync(): Promise<AiConsentDialogTestContext> {
    const dialogRef = { close: vi.fn() };
    await TestBed.configureTestingModule({
        imports: [AiConsentDialogComponent],
        providers: [provideTranslateTesting(), { provide: FdUiDialogRef, useValue: dialogRef }],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiConsentDialogComponent);
    return { component: fixture.componentInstance, dialogRef, fixture };
}

describe('AiConsentDialogComponent', () => {
    it('tracks the rendered checkbox, accepts agreement and still closes when cancelled', async () => {
        const { component, dialogRef, fixture } = await setupAiConsentDialogAsync();
        fixture.detectChanges();

        component['onAccept']();
        expect(dialogRef.close).not.toHaveBeenCalled();

        const checkbox = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('#ai-consent-agree');
        if (checkbox === null) {
            throw new Error('Expected the rendered consent checkbox');
        }
        checkbox.click();
        fixture.detectChanges();
        expect(component['isAgreed']()).toBe(true);
        component['onAccept']();
        component['onCancel']();

        expect(dialogRef.close).toHaveBeenNthCalledWith(1, true);
        expect(dialogRef.close).toHaveBeenNthCalledWith(2, false);
    });
});
