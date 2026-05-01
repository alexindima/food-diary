import { FormsModule } from '@angular/forms';
import type { Meta, StoryObj } from '@storybook/angular';
import { moduleMetadata } from '@storybook/angular';

import { FdUiSelectComponent } from './fd-ui-select.component';

const meta: Meta<FdUiSelectComponent<string>> = {
    title: 'Components/Select',
    component: FdUiSelectComponent,
    tags: ['autodocs'],
    decorators: [moduleMetadata({ imports: [FormsModule] })],
    argTypes: {
        label: { control: 'text' },
        placeholder: { control: 'text' },
        error: { control: 'text' },
        required: { control: 'boolean' },
        size: { control: 'select', options: ['sm', 'md', 'lg'] },
        fillColor: { control: 'color' },
    },
};

export default meta;
type Story = StoryObj<FdUiSelectComponent<string>>;

const mealOptions = [
    { value: 'breakfast', label: 'Breakfast' },
    { value: 'lunch', label: 'Lunch' },
    { value: 'dinner', label: 'Dinner' },
    { value: 'snack', label: 'Snack' },
];

export const Default: Story = {
    args: {
        label: 'Meal Type',
        placeholder: 'Select meal',
        options: mealOptions,
        size: 'md',
    },
};

export const WithHints: Story = {
    args: {
        label: 'Activity Level',
        placeholder: 'Select activity',
        options: [
            { value: 'sedentary', label: 'Sedentary', hint: 'Little or no exercise' },
            { value: 'light', label: 'Light', hint: '1-3 days/week' },
            { value: 'moderate', label: 'Moderate', hint: '3-5 days/week' },
            { value: 'active', label: 'Active', hint: '6-7 days/week' },
        ],
        size: 'md',
    },
};

export const WithError: Story = {
    args: {
        label: 'Category',
        placeholder: 'Select category',
        options: mealOptions,
        error: 'Category is required',
        size: 'md',
    },
};

export const Required: Story = {
    args: {
        label: 'Meal Type',
        placeholder: 'Select meal',
        options: mealOptions,
        required: true,
        size: 'md',
    },
};

export const Small: Story = {
    args: {
        label: 'Size',
        placeholder: 'Select',
        options: mealOptions,
        size: 'sm',
    },
};

export const Large: Story = {
    args: {
        label: 'Size',
        placeholder: 'Select',
        options: mealOptions,
        size: 'lg',
    },
};
