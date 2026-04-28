import type { Meta, StoryObj } from '@storybook/angular';
import { FdUiFormErrorComponent } from './fd-ui-form-error.component';

const meta: Meta<FdUiFormErrorComponent> = {
    title: 'Components/FormError',
    component: FdUiFormErrorComponent,
    tags: ['autodocs'],
    argTypes: {
        error: { control: 'text', description: 'Error message to display' },
    },
};

export default meta;
type Story = StoryObj<FdUiFormErrorComponent>;

export const Default: Story = {
    args: {
        error: 'This field is required',
    },
};

export const EmailError: Story = {
    args: {
        error: 'Please enter a valid email address',
    },
};

export const CustomError: Story = {
    args: {
        error: 'Value must be between 0 and 5000',
    },
};

export const WithInput: Story = {
    render: () => ({
        template: `
            <div style="max-width: 400px;">
                <fd-ui-input label="Email" placeholder="Enter email" error="Invalid email address"></fd-ui-input>
            </div>
        `,
    }),
};

export const MultipleErrors: Story = {
    render: () => ({
        template: `
            <div style="display: flex; flex-direction: column; gap: var(--fd-space-md); max-width: 400px;">
                <fd-ui-input label="Name" error="Name is required"></fd-ui-input>
                <fd-ui-input label="Email" error="Invalid email format"></fd-ui-input>
                <fd-ui-input label="Weight" type="number" error="Must be greater than 0"></fd-ui-input>
            </div>
        `,
    }),
};
