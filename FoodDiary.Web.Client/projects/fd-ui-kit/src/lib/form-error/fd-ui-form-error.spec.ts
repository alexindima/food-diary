import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, maxLengthError, type ValidationError } from '@angular/forms/signals';
import { TranslateService, type TranslationObject } from '@ngx-translate/core';
import { Subject } from 'rxjs';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import {
    FD_VALIDATION_ERRORS,
    FdUiFormErrorComponent,
    type FdUiFormErrorControlState,
    type FdValidationErrors,
    getNumberProperty,
    resolveSignalFormFieldError,
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
        imports: [TestHostComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(TestHostComponent);
    configure?.(fixture.componentInstance);
    fixture.detectChanges();
    return fixture;
}

describe('FdUiFormErrorComponent', () => {
    registerDirectErrorTests();
    registerControlErrorTests();
    registerSignalFormErrorTests();
});

function setTranslations(translations: TranslationObject): void {
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', translations);
    translate.use('en').subscribe();
}

describe('shared validation message contracts', () => {
    const messages = {
        FORM_ERRORS: {
            REQUIRED: 'Confirm this option',
            INVALID_NUMBER: 'Enter a number',
            MAX_LENGTH: 'Use up to {{ requiredLength }} characters',
            NON_EMPTY_ARRAY: 'Add at least one item',
            INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO: 'Enter at least {{ min }}',
            INVALID_MAX_AMOUNT: 'Enter at most {{ max }}',
            USER_EXISTS: 'Account already exists',
            PASSWORD: { MIN_LENGTH: 'Use at least {{ requiredLength }} characters', MATCH: 'Passwords must match' },
        },
    };

    it.each([
        { key: 'requiredTrue', details: true, expected: 'Confirm this option' },
        { key: 'invalidNumber', details: true, expected: 'Enter a number' },
        { key: 'minlength', details: { requiredLength: 8 }, expected: 'Use at least 8 characters' },
        { key: 'maxLength', details: { requiredLength: 12 }, expected: 'Use up to 12 characters' },
        { key: 'nonEmptyArray', details: true, expected: 'Add at least one item' },
        { key: 'min', details: { min: 1 }, expected: 'Enter at least 1' },
        { key: 'max', details: { max: 100 }, expected: 'Enter at most 100' },
        { key: 'userExists', details: true, expected: 'Account already exists' },
        { key: 'matchField', details: true, expected: 'Passwords must match' },
    ])('renders the localized $key error and its bounds', async ({ key, details, expected }) => {
        const control = createControlState({ errors: { [key]: details }, invalid: true, touched: true });
        const fixture = await createComponentAsync(component => {
            component.error = null;
            component.control = control;
        });
        setTranslations(messages);
        control.emitChange();
        fixture.detectChanges();

        expect(requireElement(fixture, '[role="alert"]').textContent.trim()).toBe(expected);
    });

    it('renders a real Signal Forms maxLength error with the same limit interpolation', async () => {
        await TestBed.configureTestingModule({ providers: [provideTranslateTesting()] }).compileComponents();
        setTranslations(messages);
        const field = createSignalField(maxLengthError(REQUIRED_LENGTH));
        const errors = TestBed.inject(FD_VALIDATION_ERRORS);
        const translate = TestBed.inject(TranslateService);

        expect(resolveSignalFormFieldError(field, errors, translate)).toBe('Use up to 8 characters');
    });
});

describe('form error rendering boundaries', () => {
    it('waits for error details before announcing an otherwise invalid control', async () => {
        const control = createControlState({ invalid: true, touched: true });
        const fixture = await createComponentAsync(component => {
            component.error = null;
            component.control = control;
        });
        expect(host(fixture).querySelector('[role="alert"]')).toBeNull();

        control.errors = { required: true };
        control.emitChange();
        fixture.detectChanges();
        expect(requireElement(fixture, '[role="alert"]').textContent.trim()).toBe('FORM_ERRORS.REQUIRED');
    });

    it('uses a visible translation key when a browser locale provides a message group instead of text', async () => {
        const control = createControlState({ errors: { required: true }, invalid: true, touched: true });
        const fixture = await createComponentAsync(component => {
            component.error = null;
            component.control = control;
        });
        setTranslations({ FORM_ERRORS: { REQUIRED: { description: 'Message group' } } });
        control.emitChange();
        fixture.detectChanges();
        expect(TestBed.inject(TranslateService).instant('FORM_ERRORS.REQUIRED')).toEqual({ description: 'Message group' });
        expect(requireElement(fixture, '[role="alert"]').textContent.trim()).toBe('FORM_ERRORS.REQUIRED');

        const direct = TestBed.createComponent(FdUiFormErrorComponent);
        direct.componentRef.setInput('error', 'FORM_ERRORS.REQUIRED');
        direct.detectChanges();
        const rendered = direct.nativeElement as HTMLElement;
        expect(rendered.querySelector('[role="alert"]')?.textContent.trim()).toBe('FORM_ERRORS.REQUIRED');
    });
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

function createSignalField(
    error: ValidationError,
    overrides: {
        dirty?: boolean;
        invalid?: boolean;
        touched?: boolean;
    } = {},
): FieldTree<string> {
    const state = {
        dirty: (): boolean => overrides.dirty ?? false,
        errors: (): ValidationError[] => [error],
        invalid: (): boolean => overrides.invalid ?? true,
        touched: (): boolean => overrides.touched ?? true,
    };

    return (() => state) as unknown as FieldTree<string>;
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
                    imports: [TestHostComponent],
                    providers: [provideTranslateTesting(), { provide: FD_VALIDATION_ERRORS, useValue: validationErrors }],
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

function registerSignalFormErrorTests(): void {
    describe('signal form error resolver', () => {
        it('maps Signal Forms minLength errors to the legacy minlength resolver key and params', async () => {
            await TestBed.configureTestingModule({
                imports: [],
                providers: [provideTranslateTesting()],
            }).compileComponents();
            const translate = TestBed.inject(TranslateService);
            const validationErrors: FdValidationErrors = {
                minlength: error => ({
                    key: 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
                    params: { requiredLength: getNumberProperty(error, 'requiredLength') },
                }),
            };
            const error: ValidationError = Object.assign({ kind: 'minLength' }, { minLength: REQUIRED_LENGTH });
            const field = createSignalField(error);

            expect(resolveSignalFormFieldError(field, validationErrors, translate)).toBe('FORM_ERRORS.PASSWORD.MIN_LENGTH');
        });

        it('respects showOnDirty when resolving Signal Forms errors', async () => {
            await TestBed.configureTestingModule({
                imports: [],
                providers: [provideTranslateTesting()],
            }).compileComponents();
            const translate = TestBed.inject(TranslateService);
            const validationErrors: FdValidationErrors = {
                required: () => 'FORM_ERRORS.REQUIRED',
            };
            const error: ValidationError = { kind: 'required' };
            const field = createSignalField(error, { dirty: true, touched: false });

            expect(resolveSignalFormFieldError(field, validationErrors, translate, { showOnDirty: false })).toBeNull();
            expect(resolveSignalFormFieldError(field, validationErrors, translate)).toBe('FORM_ERRORS.REQUIRED');
        });

        it('returns unknown for unmapped Signal Forms errors', async () => {
            await TestBed.configureTestingModule({
                imports: [],
                providers: [provideTranslateTesting()],
            }).compileComponents();
            const translate = TestBed.inject(TranslateService);
            const error: ValidationError = { kind: 'custom' };
            const field = createSignalField(error);

            expect(resolveSignalFormFieldError(field, {}, translate)).toBe('FORM_ERRORS.UNKNOWN');
        });
    });
}
