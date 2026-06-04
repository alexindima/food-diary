import type { Meta, StoryObj } from '@storybook/angular';

import { FdUiDatetimeInputComponent } from './fd-ui-datetime-input';

const meta: Meta<FdUiDatetimeInputComponent> = {
    title: 'Components/DatetimeInput',
    component: FdUiDatetimeInputComponent,
    tags: ['autodocs'],
    argTypes: {
        label: { control: 'text' },
        placeholder: { control: 'text' },
        error: { control: 'text' },
        required: { control: 'boolean' },
        size: { control: 'select', options: ['sm', 'md', 'lg'] },
    },
};

export default meta;
type Story = StoryObj<FdUiDatetimeInputComponent>;

export const Default: Story = {
    args: {
        label: 'Date & Time',
        placeholder: 'Select date and time',
        size: 'md',
    },
};

export const WithValue: Story = {
    render: () => ({
        template: '<fd-ui-datetime-input label="Meal Time" [value]="\'2024-03-15T12:30\'"></fd-ui-datetime-input>',
    }),
};

export const WithError: Story = {
    args: {
        label: 'Appointment',
        error: 'Date and time are required',
        size: 'md',
    },
};

export const Required: Story = {
    args: {
        label: 'Consumption Time',
        required: true,
        size: 'md',
    },
};
