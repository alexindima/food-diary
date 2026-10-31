import { signal } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular-vite';

import { FdUiSegmentedToggleComponent } from './fd-ui-segmented-toggle';

const meta: Meta<FdUiSegmentedToggleComponent> = {
    title: 'Components/SegmentedToggle',
    component: FdUiSegmentedToggleComponent,
    tags: ['autodocs'],
    argTypes: {
        size: { control: 'select', options: ['compact', 'sm', 'md'] },
    },
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

export const Small: Story = {
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
            template:
                '<fd-ui-segmented-toggle size="sm" [options]="options" [(selectedValue)]="selected" ariaLabel="Small period"></fd-ui-segmented-toggle>',
        };
    },
};

export const CompactChartRange: Story = {
    render: () => {
        const selected = signal('3');
        return {
            props: {
                options: [
                    { label: '3 days', value: '3' },
                    { label: '7 days', value: '7' },
                ],
                selected,
            },
            template: `
                <fd-ui-segmented-toggle
                    size="compact"
                    appearance="soft"
                    [fitContent]="true"
                    [stackOnNarrow]="false"
                    [options]="options"
                    [(selectedValue)]="selected"
                    ariaLabel="Chart period"
                    style="--fd-segmented-toggle-active-background: var(--fd-bg-surface-raised); --fd-segmented-toggle-active-color: var(--fd-color-text); --fd-segmented-toggle-active-shadow: var(--fd-shadow-sm)"
                />
            `,
        };
    },
};

export const CompactChartMetrics: Story = {
    render: () => {
        const selected = signal('calories');
        return {
            props: {
                options: [
                    { label: 'Калории', value: 'calories' },
                    { label: 'Нутриенты', value: 'macros' },
                    { label: 'Соотношение', value: 'distribution' },
                ],
                selected,
            },
            template: `
                <fd-ui-segmented-toggle
                    size="compact"
                    appearance="soft"
                    [fullWidth]="true"
                    [stackOnNarrow]="false"
                    [options]="options"
                    [(selectedValue)]="selected"
                    ariaLabel="Chart metric"
                />
            `,
        };
    },
};
