import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
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
});
