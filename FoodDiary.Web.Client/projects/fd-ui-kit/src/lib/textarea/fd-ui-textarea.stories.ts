import type { Meta, StoryObj } from '@storybook/angular';
import { FormsModule } from '@angular/forms';
import { moduleMetadata } from '@storybook/angular';
import { FdUiTextareaComponent } from './fd-ui-textarea.component';

const meta: Meta<FdUiTextareaComponent> = {
    title: 'Components/Textarea',
    component: FdUiTextareaComponent,
    tags: ['autodocs'],
    decorators: [moduleMetadata({ imports: [FormsModule] })],
    argTypes: {
        label: { control: 'text' },
        placeholder: { control: 'text' },
        error: { control: 'text' },
        required: { control: 'boolean' },
        readonly: { control: 'boolean' },
        rows: { control: 'number' },
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
            '<fd-ui-textarea label="Notes" [ngModel]="\'This is a pre-filled textarea with some content.\'" [rows]="4"></fd-ui-textarea>',
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
            '<fd-ui-textarea label="Readonly" [ngModel]="\'This content cannot be edited.\'" [readonly]="true" [rows]="3"></fd-ui-textarea>',
    }),
};
