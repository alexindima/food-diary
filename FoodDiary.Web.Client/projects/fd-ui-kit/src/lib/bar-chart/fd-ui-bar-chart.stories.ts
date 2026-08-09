import type { Meta, StoryObj } from '@storybook/angular';

import { FdUiBarChartComponent } from './fd-ui-bar-chart';

const CHART_MAXIMUM = 2500;
const THREE_QUARTER_TICK = 1875;
const HALF_TICK = 1250;
const QUARTER_TICK = 625;

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

export const StackedTimeSeries: Story = {
    args: {
        title: 'Nutrition dynamics',
        layout: 'stacked',
        axisUnit: 'kcal',
        axisTicks: [CHART_MAXIMUM, THREE_QUARTER_TICK, HALF_TICK, QUARTER_TICK, 0],
        scaleMaximum: CHART_MAXIMUM,
        referenceLines: [{ value: 2258, label: 'Goal\n2,258', labelPlacement: 'outside' }],
        categories: [
            {
                label: '3\nAug',
                values: [
                    { label: 'Protein', value: 380, color: 'var(--fd-color-primary-500)' },
                    { label: 'Fat', value: 520, color: 'var(--fd-color-orange-500)' },
                    { label: 'Carbs', value: 1120, color: 'var(--fd-color-sky-500)' },
                    { label: 'Fiber', value: 90, color: 'var(--fd-color-rose-500)' },
                ],
            },
            { label: '4\nAug', values: [] },
            {
                label: '5\nAug',
                highlighted: true,
                values: [
                    { label: 'Protein', value: 240, color: 'var(--fd-color-primary-500)' },
                    { label: 'Fat', value: 330, color: 'var(--fd-color-orange-500)' },
                    { label: 'Carbs', value: 820, color: 'var(--fd-color-sky-500)' },
                    { label: 'Fiber', value: 60, color: 'var(--fd-color-rose-500)' },
                ],
            },
        ],
    },
};
