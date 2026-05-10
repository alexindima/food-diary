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
    function host(fixture: ComponentFixture<TestHostComponent>): HTMLElement {
        return fixture.nativeElement as HTMLElement;
    }

    function requireElement<T extends Element>(fixture: ComponentFixture<TestHostComponent>, selector: string): T {
        const element = host(fixture).querySelector<T>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    }

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
        const errorText = requireElement<HTMLElement>(fixture, '.fd-ui-form-error__text');

        expect(errorText.getAttribute('role')).toBe('alert');
        expect(errorText.getAttribute('aria-live')).toBe('assertive');
    });
});
