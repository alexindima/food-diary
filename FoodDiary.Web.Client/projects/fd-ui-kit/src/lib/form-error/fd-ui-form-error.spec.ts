import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { Subject } from 'rxjs';
import { describe, expect, it } from 'vitest';

import {
    FD_VALIDATION_ERRORS,
    FdUiFormErrorComponent,
    type FdUiFormErrorControlState,
    type FdValidationErrors,
    getNumberProperty,
} from './fd-ui-form-error';

const REQUIRED_LENGTH = 8;

@Component({
    imports: [FdUiFormErrorComponent],
    template: '<fd-ui-form-error [control]="control" [error]="error" [showOnDirty]="showOnDirty"></fd-ui-form-error>',
})
class TestHostComponent {
    public error: string | null = 'FORM_ERRORS.UNKNOWN';
    public control: TestControlState | null = null;
    public showOnDirty = false;
}

type TestControlState = FdUiFormErrorControlState & {
    emitChange: () => void;
};

function createControlState(overrides: Partial<FdUiFormErrorControlState> = {}): TestControlState {
    const changes = new Subject<void>();

    return {
        dirty: false,
        emitChange: (): void => {
            changes.next();
        },
        errors: null,
        events: changes.asObservable(),
        invalid: false,
        statusChanges: changes.asObservable(),
        touched: false,
        valueChanges: changes.asObservable(),
        ...overrides,
    };
}

function host(fixture: ComponentFixture<TestHostComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function requireElement(fixture: ComponentFixture<TestHostComponent>, selector: string): HTMLElement {
    const element = host(fixture).querySelector<HTMLElement>(selector);
    if (element === null) {
        throw new Error(`Expected element ${selector} to exist.`);
    }

    return element;
}

async function createComponentAsync(configure?: (component: TestHostComponent) => void): Promise<ComponentFixture<TestHostComponent>> {
    await TestBed.configureTestingModule({
        imports: [TranslateModule.forRoot(), TestHostComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(TestHostComponent);
    configure?.(fixture.componentInstance);
    fixture.detectChanges();
    return fixture;
}

describe('FdUiFormErrorComponent', () => {
    registerDirectErrorTests();
    registerControlErrorTests();
});

function registerDirectErrorTests(): void {
    describe('direct error', () => {
        it('renders an alert live region for the error message', async () => {
            const fixture = await createComponentAsync();
            const errorText = requireElement(fixture, '.fd-ui-form-error__text');

            expect(errorText.getAttribute('role')).toBe('alert');
            expect(errorText.getAttribute('aria-live')).toBe('assertive');
        });

        it('does not render when direct error is empty', async () => {
            const fixture = await createComponentAsync(component => {
                component.error = '';
            });

            expect(host(fixture).querySelector('.fd-ui-form-error__text')).toBeNull();
        });

        it('extracts numeric validation property only from object values', () => {
            expect(getNumberProperty({ requiredLength: REQUIRED_LENGTH }, 'requiredLength')).toBe(REQUIRED_LENGTH);
            expect(getNumberProperty({ requiredLength: '8' }, 'requiredLength')).toBeUndefined();
            expect(getNumberProperty(null, 'requiredLength')).toBeUndefined();
            expect(getNumberProperty('error', 'requiredLength')).toBeUndefined();
        });
    });
}

function registerControlErrorTests(): void {
    describe('control error', () => {
        it('renders configured control error after touch', async () => {
            const control = createControlState({
                errors: { required: true },
                invalid: true,
            });
            const fixture = await createComponentAsync(component => {
                component.error = null;
                component.control = control;
            });

            expect(host(fixture).querySelector('.fd-ui-form-error__text')).toBeNull();

            control.touched = true;
            control.emitChange();
            fixture.detectChanges();

            const errorText = requireElement(fixture, '.fd-ui-form-error__text');
            expect(errorText.textContent.trim()).toBe('FORM_ERRORS.REQUIRED');
        });

        it('renders configured control error when dirty is enabled', async () => {
            const control = createControlState({
                dirty: true,
                errors: { email: true },
                invalid: true,
            });
            const fixture = await createComponentAsync(component => {
                component.error = null;
                component.control = control;
                component.showOnDirty = true;
            });

            fixture.detectChanges();

            const errorText = requireElement(fixture, '.fd-ui-form-error__text');
            expect(errorText.textContent.trim()).toBe('FORM_ERRORS.EMAIL');
        });

        it('renders unknown message for unmapped control error', async () => {
            const control = createControlState({
                errors: { custom: true },
                invalid: true,
                touched: true,
            });
            const fixture = await createComponentAsync(component => {
                component.error = null;
                component.control = control;
            });

            const errorText = requireElement(fixture, '.fd-ui-form-error__text');
            expect(errorText.textContent.trim()).toBe('FORM_ERRORS.UNKNOWN');
        });

        it('merges resolver params with component context', async () => {
            const validationErrors: FdValidationErrors = {
                server: (): string => 'FORM_ERRORS.SERVER',
            };
            await TestBed.resetTestingModule()
                .configureTestingModule({
                    imports: [TranslateModule.forRoot(), TestHostComponent],
                    providers: [{ provide: FD_VALIDATION_ERRORS, useValue: validationErrors }],
                })
                .compileComponents();
            const fixture = TestBed.createComponent(TestHostComponent);
            const control = createControlState({
                errors: { server: { reason: 'duplicate' } },
                invalid: true,
                touched: true,
            });
            fixture.componentInstance.error = null;
            fixture.componentInstance.control = control;
            fixture.detectChanges();

            const errorText = requireElement(fixture, '.fd-ui-form-error__text');
            expect(errorText.textContent.trim()).toBe('FORM_ERRORS.SERVER');
        });
    });
}
