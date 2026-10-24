import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { form, FormField, required } from '@angular/forms/signals';
import type { Meta, StoryObj } from '@storybook/angular-vite';
import { firstValueFrom, timer } from 'rxjs';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiInputComponent } from '../input/fd-ui-input';
import { FdUiFormDirective } from './fd-ui-form.directive';

const SAVE_DELAY_MS = 150;

@Component({
    selector: 'fd-form-validation-example',
    imports: [FdUiFormDirective, FormField, FdUiInputComponent, FdUiButtonComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <form class="fd-stack fd-gap-md" [formRoot]="fields">
            <fd-ui-input label="Name" [formField]="fields.name" />
            <fd-ui-input label="Email" type="email" [formField]="fields.email" />
            <fd-ui-button type="submit" [loading]="fields().submitting()">Save</fd-ui-button>
            @if (saved()) {
                <p role="status">Saved</p>
            }
        </form>
    `,
})
class FormValidationExample {
    protected readonly model = signal({ name: '', email: '' });
    protected readonly saved = signal(false);
    private readonly submitAsync = async (): Promise<void> => {
        await firstValueFrom(timer(SAVE_DELAY_MS));
        this.saved.set(true);
    };
    protected readonly fields = form(
        this.model,
        path => {
            required(path.name);
            required(path.email);
        },
        {
            submission: {
                action: this.submitAsync,
            },
        },
    );
}

export default {
    title: 'Behavior/Form validation',
    component: FormValidationExample,
    tags: ['autodocs'],
} satisfies Meta<FormValidationExample>;

export const SubmitErrors: StoryObj<FormValidationExample> = {};
