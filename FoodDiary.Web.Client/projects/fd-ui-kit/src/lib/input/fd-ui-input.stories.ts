import { FormsModule } from '@angular/forms';
import type { Meta, StoryObj } from '@storybook/angular';
import { moduleMetadata } from '@storybook/angular';

import { FdUiInputComponent } from './fd-ui-input.component';

const meta: Meta<FdUiInputComponent> = {
    title: 'Components/Input',
    component: FdUiInputComponent,
    tags: ['autodocs'],
    decorators: [moduleMetadata({ imports: [FormsModule] })],
    argTypes: {
        label: { control: 'text' },
        placeholder: { control: 'text' },
        type: {
            control: 'select',
            options: ['text', 'number', 'password', 'email', 'tel'],
        },
        error: { control: 'text' },
        required: { control: 'boolean' },
        readonly: { control: 'boolean' },
        maxLength: { control: 'number' },
        size: { control: 'select', options: ['sm', 'md', 'lg'] },
        prefixIcon: { control: 'text' },
        suffixButtonIcon: { control: 'text' },
        fillColor: { control: 'color' },
    },
};

export default meta;
type Story = StoryObj<FdUiInputComponent>;

export const Default: Story = {
    args: {
        label: 'Name',
        placeholder: 'Enter your name',
        size: 'md',
    },
};

export const WithValue: Story = {
    render: () => ({
        template: '<fd-ui-input label="Name" placeholder="Enter your name" [ngModel]="\'John Doe\'"></fd-ui-input>',
    }),
};

export const WithError: Story = {
    args: {
        label: 'Email',
        placeholder: 'Enter email',
        error: 'Invalid email address',
        size: 'md',
    },
};

export const Required: Story = {
    args: {
        label: 'Username',
        placeholder: 'Required field',
        required: true,
        size: 'md',
    },
};

export const Readonly: Story = {
    render: () => ({
        template: '<fd-ui-input label="Status" [ngModel]="\'Active\'" [readonly]="true"></fd-ui-input>',
    }),
};

export const Password: Story = {
    args: {
        label: 'Password',
        placeholder: 'Enter password',
        type: 'password',
        size: 'md',
    },
};

export const Number: Story = {
    args: {
        label: 'Calories',
        placeholder: '0',
        type: 'number',
        step: 1,
        size: 'md',
    },
};

export const WithPrefixIcon: Story = {
    args: {
        label: 'Search',
        placeholder: 'Search products...',
        prefixIcon: 'search',
        size: 'md',
    },
};

export const WithSuffixButton: Story = {
    args: {
        label: 'Password',
        placeholder: 'Enter password',
        type: 'password',
        suffixButtonIcon: 'visibility',
        suffixButtonAriaLabel: 'Toggle password visibility',
        size: 'md',
    },
};

export const Small: Story = {
    args: { label: 'Small Input', placeholder: 'Small', size: 'sm' },
};

export const Large: Story = {
    args: { label: 'Large Input', placeholder: 'Large', size: 'lg' },
};

export const WithMaxLength: Story = {
    args: {
        label: 'Short text',
        placeholder: 'Max 20 chars',
        maxLength: 20,
        size: 'md',
    },
};

export const WithFillColor: Story = {
    args: {
        label: 'Highlighted',
        placeholder: 'Custom background',
        fillColor: '#f0f7ff',
        size: 'md',
    },
};

export const AllSizes: Story = {
    render: () => ({
        template: `
            <div style="display: flex; flex-direction: column; gap: var(--fd-space-md); max-width: 400px;">
                <fd-ui-input label="Small" placeholder="sm" size="sm"></fd-ui-input>
                <fd-ui-input label="Medium" placeholder="md" size="md"></fd-ui-input>
                <fd-ui-input label="Large" placeholder="lg" size="lg"></fd-ui-input>
            </div>
        `,
    }),
};
