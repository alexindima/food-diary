import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { AiConsentDialogComponent } from './ai-consent-dialog.component';

type AiConsentDialogTestContext = {
    component: AiConsentDialogComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<AiConsentDialogComponent>;
};

async function setupAiConsentDialogAsync(): Promise<AiConsentDialogTestContext> {
    const dialogRef = { close: vi.fn() };
    await TestBed.configureTestingModule({
        imports: [AiConsentDialogComponent, TranslateModule.forRoot()],
        providers: [{ provide: FdUiDialogRef, useValue: dialogRef }],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiConsentDialogComponent);
    return { component: fixture.componentInstance, dialogRef, fixture };
}

describe('AiConsentDialogComponent', () => {
    it('tracks agreement checkbox state', async () => {
        const { component, fixture } = await setupAiConsentDialogAsync();
        const input = document.createElement('input');
        input.type = 'checkbox';
        input.checked = true;
        fixture.detectChanges();

        component.onCheckboxChange({ target: input } as unknown as Event);

        expect(component.isAgreed()).toBe(true);
    });

    it('closes with consent result', async () => {
        const { component, dialogRef, fixture } = await setupAiConsentDialogAsync();
        fixture.detectChanges();

        component.onAccept();
        component.onCancel();

        expect(dialogRef.close).toHaveBeenNthCalledWith(1, true);
        expect(dialogRef.close).toHaveBeenNthCalledWith(2, false);
    });
});
