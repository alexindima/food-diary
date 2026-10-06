import type { Meta, StoryObj } from '@storybook/angular-vite';

import { FdUiTextareaComponent } from './fd-ui-textarea';

const meta: Meta<FdUiTextareaComponent> = {
    title: 'Components/Textarea',
    component: FdUiTextareaComponent,
    tags: ['autodocs'],
    argTypes: {
        label: { control: 'text' },
        placeholder: { control: 'text' },
        error: { control: 'text' },
        required: { control: 'boolean' },
        readonly: { control: 'boolean' },
        rows: { control: 'number' },
        stretch: { control: 'boolean' },
        maxLength: { control: 'number' },
        size: { control: 'select', options: ['sm', 'md', 'lg'] },
        fillColor: { control: 'color' },
    },
};

export default meta;
type Story = StoryObj<FdUiTextareaComponent>;

export const Default: Story = {
    args: {
        label: 'Description',
        placeholder: 'Enter description...',
        rows: 4,
        size: 'md',
    },
};

export const WithValue: Story = {
    render: () => ({
        template:
            '<fd-ui-textarea label="Notes" [value]="\'This is a pre-filled textarea with some content.\'" [rows]="4"></fd-ui-textarea>',
    }),
};

export const WithError: Story = {
    args: {
        label: 'Comment',
        placeholder: 'Leave a comment',
        error: 'Comment is required',
        rows: 3,
        size: 'md',
    },
};

export const Required: Story = {
    args: {
        label: 'Recipe Instructions',
        placeholder: 'Describe how to prepare...',
        required: true,
        rows: 6,
        size: 'md',
    },
};

export const WithMaxLength: Story = {
    args: {
        label: 'Short note',
        placeholder: 'Max 100 characters',
        maxLength: 100,
        rows: 3,
        size: 'md',
    },
};

export const Readonly: Story = {
    render: () => ({
        template:
            '<fd-ui-textarea label="Readonly" [value]="\'This content cannot be edited.\'" [readonly]="true" [rows]="3"></fd-ui-textarea>',
    }),
};

export const Stretch: Story = {
    render: () => ({
        template:
            '<div style="display: flex; height: 300px"><fd-ui-textarea style="flex: 1" label="Step description" [stretch]="true" /></div>',
    }),
};
