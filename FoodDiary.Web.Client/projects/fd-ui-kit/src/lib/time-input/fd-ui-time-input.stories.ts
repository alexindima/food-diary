import { FormsModule } from '@angular/forms';
import type { Meta, StoryObj } from '@storybook/angular';
import { moduleMetadata } from '@storybook/angular';

import { FdUiTimeInputComponent } from './fd-ui-time-input.component';

const meta: Meta<FdUiTimeInputComponent> = {
    title: 'Components/TimeInput',
    component: FdUiTimeInputComponent,
    tags: ['autodocs'],
    decorators: [moduleMetadata({ imports: [FormsModule] })],
    argTypes: {
        label: { control: 'text' },
        placeholder: { control: 'text' },
        error: { control: 'text' },
        required: { control: 'boolean' },
        size: { control: 'select', options: ['sm', 'md', 'lg'] },
    },
};

export default meta;
type Story = StoryObj<FdUiTimeInputComponent>;

export const Default: Story = {
    args: {
        label: 'Time',
        placeholder: 'HH:MM',
        size: 'md',
    },
};

export const WithValue: Story = {
    render: () => ({
        template: '<fd-ui-time-input label="Meal Time" [ngModel]="\'12:30\'"></fd-ui-time-input>',
    }),
};

export const WithError: Story = {
    args: {
        label: 'Time',
        error: 'Invalid time format',
        size: 'md',
    },
};

export const Required: Story = {
    args: {
        label: 'Reminder Time',
        placeholder: 'HH:MM',
        required: true,
        size: 'md',
    },
};
