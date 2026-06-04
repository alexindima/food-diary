import type { Meta, StoryObj } from '@storybook/angular';

import { FdUiDateInputComponent } from './fd-ui-date-input';

const meta: Meta<FdUiDateInputComponent> = {
    title: 'Components/DateInput',
    component: FdUiDateInputComponent,
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
type Story = StoryObj<FdUiDateInputComponent>;

export const Default: Story = {
    args: {
        label: 'Date',
        placeholder: 'Select date',
        size: 'md',
    },
};

export const WithValue: Story = {
    render: () => ({
        template: '<fd-ui-date-input label="Birth Date" [value]="\'2000-01-15\'"></fd-ui-date-input>',
    }),
};

export const WithError: Story = {
    args: {
        label: 'Entry Date',
        placeholder: 'Select date',
        error: 'Date is required',
        size: 'md',
    },
};

export const Required: Story = {
    args: {
        label: 'Measurement Date',
        placeholder: 'Required',
        required: true,
        size: 'md',
    },
};

export const Small: Story = {
    args: { label: 'Date', size: 'sm' },
};

export const Large: Story = {
    args: { label: 'Date', size: 'lg' },
};
