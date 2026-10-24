import { CdkTextareaAutosize } from '@angular/cdk/text-field';
import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    effect,
    type ElementRef,
    input,
    model,
    output,
    signal,
    viewChild,
} from '@angular/core';
import type { FormValueControl, ValidationError } from '@angular/forms/signals';

import { injectFdUiFieldError } from '../form-error/fd-ui-signal-field-error';
import type { FdUiEnterKeyHint } from '../types/enter-key-hint.type';
import type { FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;
const DEFAULT_ROWS = 4;

@Component({
    selector: 'fd-ui-textarea',
    imports: [CommonModule, CdkTextareaAutosize],
    templateUrl: './fd-ui-textarea.html',
    styleUrls: ['./fd-ui-textarea.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { '[class.fd-ui-textarea-stretch]': 'stretch()', '[attr.id]': 'null' },
})
export class FdUiTextareaComponent implements FormValueControl<string | number | null> {
    private readonly control = viewChild.required<ElementRef<HTMLTextAreaElement>>('control');
    public readonly id = input(`fd-ui-textarea-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly enterKeyHint = input<FdUiEnterKeyHint>();
    public readonly error = input<string | null>();
    public readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);
    public readonly required = input(false);
    public readonly readonly = input(false);
    public readonly rows = input(DEFAULT_ROWS);
    public readonly autosize = input(false);
    public readonly stretch = input(false);
    public readonly maxLength = input<number>();
    public readonly maximumLength = input<number>();
    public readonly size = input<FdUiFieldSize>('md');
    public readonly fillColor = input<string | null>(null);
    public readonly value = model<string | number | null>(null);
    public readonly touched = model(false);
    protected readonly resolvedError = injectFdUiFieldError(this.error, this.errors, this.touched);
    public readonly touch = output();
    public readonly disabled = input(false);

    protected readonly internalValue = signal('');
    protected readonly isFocused = signal(false);

    public constructor() {
        effect(() => {
            const value = this.value();
            this.internalValue.set(value === null ? '' : String(value));
        });
    }

    protected readonly sizeClass = computed(() => `fd-ui-textarea--size-${this.size()}`);
    protected readonly shouldFloatLabel = computed(() => this.isFocused() || this.internalValue().trim().length > 0);
    protected readonly hasError = computed(() => {
        const error = this.resolvedError();

        return typeof error === 'string' && error.trim().length > 0;
    });
    protected readonly hostClass = computed(
        () =>
            `fd-ui-textarea ${this.sizeClass()}${this.hasError() ? ' fd-ui-textarea--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-textarea--floating' : ''}`,
    );
    protected readonly shouldShowPlaceholder = computed(() => this.isFocused() && this.internalValue().trim().length === 0);
    protected readonly placeholderAttribute = computed(() => (this.shouldShowPlaceholder() ? (this.placeholder() ?? null) : null));

    protected onInput(value: string): void {
        if (this.disabled()) {
            return;
        }

        this.internalValue.set(value);
        this.value.set(value);
    }

    protected onBlur(): void {
        this.isFocused.set(false);
        this.touched.set(true);
        this.touch.emit();
    }

    protected onFocus(): void {
        this.isFocused.set(true);
    }

    protected focusControl(control: HTMLTextAreaElement): void {
        if (this.disabled()) {
            return;
        }

        control.focus();
    }

    /** Focuses the native control without exposing its markup to consumers. */
    // eslint-disable-next-line local/prefer-protected-template-members -- Public focus contract used by interaction directives.
    public focus(): void {
        if (!this.disabled()) {
            this.control().nativeElement.focus();
        }
    }
}
