import type { Meta, StoryObj } from '@storybook/angular';
import { FdUiDialogShellComponent } from '../dialog-shell/fd-ui-dialog-shell.component';

const meta: Meta<FdUiDialogShellComponent> = {
    title: 'Components/Dialog',
    component: FdUiDialogShellComponent,
    tags: ['autodocs'],
    argTypes: {
        title: { control: 'text' },
        subtitle: { control: 'text' },
        size: { control: 'select', options: ['sm', 'md', 'lg'] },
        dismissible: { control: 'boolean' },
        flush: { control: 'boolean' },
    },
    render: (args) => ({
        props: args,
        template: `
            <fd-ui-dialog-shell [title]="title" [subtitle]="subtitle" [size]="size" [dismissible]="dismissible" [flush]="flush">
                <p style="margin: 0; color: #666;">Dialog body content goes here. This demonstrates the dialog shell without the overlay.</p>
                <div fdUiDialogFooter style="display: flex; gap: 8px; justify-content: flex-end;">
                    <fd-ui-button variant="secondary" fill="outline">Cancel</fd-ui-button>
                    <fd-ui-button variant="primary">Confirm</fd-ui-button>
                </div>
            </fd-ui-dialog-shell>
        `,
    }),
};

export default meta;
type Story = StoryObj<FdUiDialogShellComponent>;

export const Small: Story = {
    args: {
        title: 'Delete Item',
        subtitle: 'This action cannot be undone',
        size: 'sm',
        dismissible: true,
    },
};

export const Medium: Story = {
    args: {
        title: 'Edit Product',
        subtitle: 'Update product nutrition info',
        size: 'md',
        dismissible: true,
    },
};

export const Large: Story = {
    args: {
        title: 'Create Recipe',
        subtitle: 'Add a new recipe with ingredients',
        size: 'lg',
        dismissible: true,
    },
};

export const NonDismissible: Story = {
    args: {
        title: 'Confirm Action',
        size: 'sm',
        dismissible: false,
    },
};

export const Flush: Story = {
    args: {
        title: 'Select Items',
        size: 'md',
        flush: true,
        dismissible: true,
    },
};

export const ConfirmDelete: Story = {
    render: () => ({
        template: `
            <fd-ui-dialog-shell title="Delete Product" size="sm" [dismissible]="true">
                <p style="margin: 0; color: #666;">Are you sure you want to delete "Chicken Breast"? This action cannot be undone.</p>
                <div fdUiDialogFooter style="display: flex; gap: 8px; justify-content: flex-end;">
                    <fd-ui-button variant="secondary" fill="outline">Cancel</fd-ui-button>
                    <fd-ui-button variant="danger">Delete</fd-ui-button>
                </div>
            </fd-ui-dialog-shell>
        `,
    }),
};

export const WithForm: Story = {
    render: () => ({
        template: `
            <fd-ui-dialog-shell title="Add Weight Entry" size="md" [dismissible]="true">
                <div style="display: flex; flex-direction: column; gap: 16px;">
                    <fd-ui-date-input label="Date"></fd-ui-date-input>
                    <fd-ui-input label="Weight" placeholder="0.0" type="number" step="0.1"></fd-ui-input>
                </div>
                <div fdUiDialogFooter style="display: flex; gap: 8px; justify-content: flex-end;">
                    <fd-ui-button variant="secondary" fill="outline">Cancel</fd-ui-button>
                    <fd-ui-button variant="primary">Save</fd-ui-button>
                </div>
            </fd-ui-dialog-shell>
        `,
    }),
};

export const AllSizes: Story = {
    render: () => ({
        template: `
            <div style="display: flex; flex-direction: column; gap: 24px;">
                <fd-ui-dialog-shell title="Small Dialog" size="sm"><p style="margin:0;color:#666">Compact content</p></fd-ui-dialog-shell>
                <fd-ui-dialog-shell title="Medium Dialog" size="md"><p style="margin:0;color:#666">Standard content area</p></fd-ui-dialog-shell>
                <fd-ui-dialog-shell title="Large Dialog" size="lg"><p style="margin:0;color:#666">Spacious content area for complex forms</p></fd-ui-dialog-shell>
            </div>
        `,
    }),
};
