import type { Meta, StoryObj } from '@storybook/angular-vite';

import { FdUiChartLegendComponent } from './fd-ui-chart-legend';

const meta: Meta<FdUiChartLegendComponent> = {
    title: 'Components/Charts/Legend',
    component: FdUiChartLegendComponent,
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<FdUiChartLegendComponent>;

export const Default: Story = {
    args: {
        items: [
            { label: 'Белки', color: 'var(--fd-color-nutrition-proteins)' },
            { label: 'Жиры', color: 'var(--fd-color-nutrition-fats)' },
            { label: 'Углеводы', color: 'var(--fd-color-nutrition-carbs)' },
            { label: 'Клетчатка', color: 'var(--fd-color-nutrition-fiber)' },
        ],
    },
};

export const LongLabels: Story = {
    args: {
        items: [
            { label: 'A long localized nutrient label that wraps', color: 'var(--fd-color-nutrition-proteins)' },
            { label: 'Длиннаяподписьбезпробелов', color: 'var(--fd-color-nutrition-carbs)' },
        ],
    },
};

export const ReservedSpace: Story = {
    args: { ...Default.args, visible: false },
};
