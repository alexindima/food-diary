import type { Meta, StoryObj } from '@storybook/angular';
import { signal } from '@angular/core';
import { FdUiSegmentedToggleComponent } from './fd-ui-segmented-toggle.component';

const meta: Meta<FdUiSegmentedToggleComponent> = {
    title: 'Components/SegmentedToggle',
    component: FdUiSegmentedToggleComponent,
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<FdUiSegmentedToggleComponent>;

export const Default: Story = {
    render: () => {
        const selected = signal('week');
        return {
            props: {
                options: [
                    { label: 'Day', value: 'day' },
                    { label: 'Week', value: 'week' },
                    { label: 'Month', value: 'month' },
                ],
                selected,
            },
            template: `
                <fd-ui-segmented-toggle [options]="options" [(selectedValue)]="selected" ariaLabel="Time period"></fd-ui-segmented-toggle>
                <p style="margin-top: 16px; color: #666;">Selected: {{ selected() }}</p>
            `,
        };
    },
};

export const TwoOptions: Story = {
    render: () => {
        const selected = signal('metric');
        return {
            props: {
                options: [
                    { label: 'Metric', value: 'metric' },
                    { label: 'Imperial', value: 'imperial' },
                ],
                selected,
            },
            template:
                '<fd-ui-segmented-toggle [options]="options" [(selectedValue)]="selected" ariaLabel="Unit system"></fd-ui-segmented-toggle>',
        };
    },
};

export const FourOptions: Story = {
    render: () => {
        const selected = signal('all');
        return {
            props: {
                options: [
                    { label: 'All', value: 'all' },
                    { label: 'Breakfast', value: 'breakfast' },
                    { label: 'Lunch', value: 'lunch' },
                    { label: 'Dinner', value: 'dinner' },
                ],
                selected,
            },
            template:
                '<fd-ui-segmented-toggle [options]="options" [(selectedValue)]="selected" ariaLabel="Meal filter"></fd-ui-segmented-toggle>',
        };
    },
};
