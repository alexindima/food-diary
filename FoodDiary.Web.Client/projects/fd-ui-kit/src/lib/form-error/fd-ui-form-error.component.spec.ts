import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { FdUiFormErrorComponent } from './fd-ui-form-error.component';

@Component({
    imports: [FdUiFormErrorComponent],
    template: '<fd-ui-form-error [error]="error"></fd-ui-form-error>',
})
class TestHostComponent {
    public error = 'FORM_ERRORS.UNKNOWN';
}

describe('FdUiFormErrorComponent', () => {
    async function createComponentAsync(): Promise<ComponentFixture<TestHostComponent>> {
        await TestBed.configureTestingModule({
            imports: [TranslateModule.forRoot(), TestHostComponent],
        }).compileComponents();

        const fixture = TestBed.createComponent(TestHostComponent);
        fixture.detectChanges();
        return fixture;
    }

    it('renders an alert live region for the error message', async () => {
        const fixture = await createComponentAsync();
        const errorText = fixture.nativeElement.querySelector('.fd-ui-form-error__text');

        expect(errorText).not.toBeNull();
        expect(errorText.getAttribute('role')).toBe('alert');
        expect(errorText.getAttribute('aria-live')).toBe('assertive');
    });
});
