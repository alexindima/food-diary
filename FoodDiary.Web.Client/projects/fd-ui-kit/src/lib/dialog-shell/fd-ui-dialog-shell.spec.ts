import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FD_UI_DIALOG_DISMISSAL_LOCK } from '../dialog/fd-ui-dialog.tokens';
import { FdUiDialogShellComponent } from './fd-ui-dialog-shell';

@Component({
    imports: [FdUiDialogShellComponent, FdUiDialogFooterDirective],
    template: `
        <fd-ui-dialog-shell title="Settings">
            <div class="test-body">Body</div>
            <div fdUiDialogFooter class="test-footer">Footer action</div>
        </fd-ui-dialog-shell>
    `,
})
class DialogShellHostComponent {}

describe('FdUiDialogShellComponent', () => {
    it('projects footer content into the dialog footer after the body', () => {
        TestBed.configureTestingModule({
            imports: [DialogShellHostComponent],
            providers: [provideTranslateTesting()],
        });

        const fixture: ComponentFixture<DialogShellHostComponent> = TestBed.createComponent(DialogShellHostComponent);
        fixture.detectChanges();

        const element = fixture.nativeElement as HTMLElement;
        const body = element.querySelector('.fd-ui-dialog__body');
        const footer = element.querySelector('.fd-ui-dialog__footer');

        expect(body?.querySelector('.test-body')).toBeTruthy();
        expect(body?.querySelector('.test-footer')).toBeNull();
        expect(footer?.querySelector('.test-footer')).toBeTruthy();
        if (body === null || footer === null) {
            throw new Error('Expected dialog body and footer to be rendered');
        }

        expect(body.compareDocumentPosition(footer) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    });

    it('forwards a changing pending-operation guard to the underlying dialog', () => {
        const release = vi.fn<() => void>();
        const lock = vi.fn<() => () => void>().mockReturnValue(release);
        TestBed.configureTestingModule({
            imports: [FdUiDialogShellComponent],
            providers: [provideTranslateTesting(), { provide: FD_UI_DIALOG_DISMISSAL_LOCK, useValue: lock }],
        });
        const fixture = TestBed.createComponent(FdUiDialogShellComponent);
        fixture.componentRef.setInput('disableClose', true);
        fixture.detectChanges();
        const button = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.fd-ui-dialog__close-button');
        expect(lock).toHaveBeenCalledOnce();
        expect(button?.disabled).toBe(true);
        fixture.componentRef.setInput('disableClose', false);
        fixture.detectChanges();
        expect(release).toHaveBeenCalledOnce();
        expect(button?.disabled).toBe(false);
    });
});
