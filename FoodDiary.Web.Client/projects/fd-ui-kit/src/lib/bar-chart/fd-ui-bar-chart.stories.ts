import type { Meta, StoryObj } from '@storybook/angular';

import { FdUiBarChartComponent } from './fd-ui-bar-chart.component';

const meta: Meta<FdUiBarChartComponent> = {
    title: 'Components/Charts/Bar Chart',
    component: FdUiBarChartComponent,
    tags: ['autodocs'],
    argTypes: {
        title: { control: 'text' },
        emptyLabel: { control: 'text' },
        showLabels: { control: 'boolean' },
    },
};

export default meta;
type Story = StoryObj<FdUiBarChartComponent>;

export const Default: Story = {
    args: {
        title: 'Macros',
        items: [
            { label: 'Protein', value: 112, color: 'var(--fd-color-blue-500)' },
            { label: 'Fat', value: 64, color: 'var(--fd-color-orange-500)' },
            { label: 'Carbs', value: 210, color: 'var(--fd-color-emerald-500)' },
        ],
        emptyLabel: 'No data',
        showLabels: true,
    },
};

export const Compact: Story = {
    args: {
        title: 'Week',
        items: [
            { label: 'M', value: 1450 },
            { label: 'T', value: 1810 },
            { label: 'W', value: 1630 },
            { label: 'T', value: 1975 },
            { label: 'F', value: 1520 },
            { label: 'S', value: 2110 },
            { label: 'S', value: 1740 },
        ],
        showLabels: false,
    },
};
