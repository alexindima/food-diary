import type { Meta, StoryObj } from '@storybook/angular';

import { FdUiLineChartComponent } from './fd-ui-line-chart';

const meta: Meta<FdUiLineChartComponent> = {
    title: 'Components/Charts/Line Chart',
    component: FdUiLineChartComponent,
    tags: ['autodocs'],
    argTypes: {
        title: { control: 'text' },
        emptyLabel: { control: 'text' },
        lineColor: { control: 'text' },
        fillColor: { control: 'text' },
        showArea: { control: 'boolean' },
        showPoints: { control: 'boolean' },
        showAxisLabels: { control: 'boolean' },
        showGrid: { control: 'boolean' },
    },
};

export default meta;
type Story = StoryObj<FdUiLineChartComponent>;

export const Default: Story = {
    args: {
        title: 'Weight trend',
        points: [
            { label: 'Mon', value: 82.4 },
            { label: 'Tue', value: 82.1 },
            { label: 'Wed', value: 81.9 },
            { label: 'Thu', value: 82 },
            { label: 'Fri', value: 81.6 },
            { label: 'Sat', value: 81.4 },
            { label: 'Sun', value: 81.2 },
        ],
        showArea: true,
        showPoints: true,
    },
};

export const Sparkline: Story = {
    args: {
        title: 'Calories',
        points: [
            { label: '1', value: 1450 },
            { label: '2', value: 1620 },
            { label: '3', value: 1580 },
            { label: '4', value: 1710 },
            { label: '5', value: 1510 },
            { label: '6', value: 1880 },
        ],
        showArea: false,
        showPoints: false,
    },
};

export const AxisLabels: Story = {
    args: {
        title: 'Calories',
        points: [
            { label: '20 мая', value: 0 },
            { label: '21 мая', value: 0 },
            { label: '22 мая', value: 0 },
            { label: '24 мая', value: 0 },
            { label: '25 мая', value: 0 },
            { label: '26 мая', value: 0 },
        ],
        defaultMaxValue: 2000,
        valueSuffix: 'ккал',
        axisDecimalPlaces: 0,
        showArea: true,
        showAxisLabels: true,
        showGrid: true,
        showPoints: true,
    },
};

export const MultiSeries: Story = {
    args: {
        title: 'Nutrients',
        series: [
            {
                label: 'Proteins',
                color: '#3b82f6',
                points: [
                    { label: 'May', value: 0 },
                    { label: 'June', value: 0 },
                    { label: 'July', value: 12 },
                    { label: 'Aug.', value: 0 },
                    { label: 'Sept.', value: 14 },
                ],
            },
            {
                label: 'Fats',
                color: '#fde68a',
                points: [
                    { label: 'May', value: 0 },
                    { label: 'June', value: 4 },
                    { label: 'July', value: 10 },
                    { label: 'Aug.', value: 0 },
                    { label: 'Sept.', value: 12 },
                ],
            },
            {
                label: 'Carbs',
                color: '#10b981',
                points: [
                    { label: 'May', value: 0 },
                    { label: 'June', value: 0 },
                    { label: 'July', value: 24 },
                    { label: 'Aug.', value: 8 },
                    { label: 'Sept.', value: 0 },
                ],
            },
            {
                label: 'Fiber',
                color: '#7c3aed',
                points: [
                    { label: 'May', value: 0 },
                    { label: 'June', value: 1 },
                    { label: 'July', value: 0 },
                    { label: 'Aug.', value: 2 },
                    { label: 'Sept.', value: 0 },
                ],
            },
        ],
        defaultMaxValue: 100,
        valueSuffix: 'g',
        axisDecimalPlaces: 0,
        showArea: false,
        showAxisLabels: true,
        showGrid: true,
        showPoints: true,
    },
};
