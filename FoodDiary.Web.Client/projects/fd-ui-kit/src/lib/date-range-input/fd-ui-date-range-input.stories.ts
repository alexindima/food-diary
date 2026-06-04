import type { Meta, StoryObj } from '@storybook/angular';

import { FdUiDateRangeInputComponent } from './fd-ui-date-range-input';

const meta: Meta<FdUiDateRangeInputComponent> = {
    title: 'Components/DateRangeInput',
    component: FdUiDateRangeInputComponent,
    tags: ['autodocs'],
    argTypes: {
        startLabel: { control: 'text' },
        endLabel: { control: 'text' },
        startPlaceholder: { control: 'text' },
        endPlaceholder: { control: 'text' },
        size: { control: 'select', options: ['sm', 'md', 'lg'] },
    },
};

export default meta;
type Story = StoryObj<FdUiDateRangeInputComponent>;

export const Default: Story = {
    args: {
        startLabel: 'Start Date',
        endLabel: 'End Date',
        startPlaceholder: 'From',
        endPlaceholder: 'To',
        size: 'md',
    },
};

export const WithLabels: Story = {
    args: {
        startLabel: 'Period Start',
        endLabel: 'Period End',
        size: 'md',
    },
};

export const Small: Story = {
    args: {
        startLabel: 'From',
        endLabel: 'To',
        size: 'sm',
    },
};
