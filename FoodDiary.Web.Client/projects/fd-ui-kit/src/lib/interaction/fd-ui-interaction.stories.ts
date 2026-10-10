import { signal } from '@angular/core';
import { type Meta, moduleMetadata, type StoryObj } from '@storybook/angular-vite';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiCardComponent } from '../card/fd-ui-card';
import { FdUiInputComponent } from '../input/fd-ui-input';
import { FdUiTextareaComponent } from '../textarea/fd-ui-textarea';
import { FdUiActivateDirective } from './fd-ui-activate.directive';
import { FdUiFocusWhenDirective } from './fd-ui-focus-when.directive';
import { FdUiPreventUnloadDirective } from './fd-ui-prevent-unload.directive';
import { FdUiStopActivationDirective } from './fd-ui-stop-activation.directive';

const meta: Meta = {
    title: 'Behavior/Interactions',
    decorators: [
        moduleMetadata({
            imports: [
                FdUiButtonComponent,
                FdUiCardComponent,
                FdUiInputComponent,
                FdUiTextareaComponent,
                FdUiActivateDirective,
                FdUiFocusWhenDirective,
                FdUiPreventUnloadDirective,
                FdUiStopActivationDirective,
            ],
        }),
    ],
};

export default meta;
type Story = StoryObj;

export const CardActivation: Story = {
    render: () => ({
        props: { activated: signal(0), nested: signal(0) },
        template: `
            <fd-ui-card fdUiActivate title="Activate with click, Enter or Space" (fdUiActivated)="activated.set(activated() + 1)">
                <p>Card activations: {{ activated() }}</p>
                <fd-ui-button fdUiStopActivation (click)="nested.set(nested() + 1)">Nested action: {{ nested() }}</fd-ui-button>
            </fd-ui-card>
        `,
    }),
};

export const FocusRequest: Story = {
    render: () => ({
        props: { request: signal(false), descriptionRequest: signal(false) },
        template: `
            <div class="fd-stack fd-gap-md">
                <fd-ui-button (click)="request.set(true)">Focus the name field</fd-ui-button>
                <fd-ui-input label="Name" [fdUiFocusWhen]="request()" (fdUiFocused)="request.set(false)" />
                <fd-ui-button (click)="descriptionRequest.set(true)">Focus the description</fd-ui-button>
                <fd-ui-textarea label="Description" [fdUiFocusWhen]="descriptionRequest()" (fdUiFocused)="descriptionRequest.set(false)" />
            </div>
        `,
    }),
};

export const UnloadProtection: Story = {
    render: () => ({
        props: { dirty: signal(false) },
        template: `
            <div [fdUiPreventUnload]="dirty()" class="fd-stack fd-gap-md">
                <p>Unsaved changes: {{ dirty() }}</p>
                <fd-ui-button (click)="dirty.set(!dirty())">Toggle unsaved changes</fd-ui-button>
            </div>
        `,
    }),
};
