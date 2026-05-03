import { type Meta, type StoryObj } from '@storybook/angular';

import { FdUiChipSelectComponent } from './fd-ui-chip-select.component';

const meta: Meta<FdUiChipSelectComponent> = {
    title: 'Selection/Chip Select',
    component: FdUiChipSelectComponent,
    args: {
        ariaLabel: 'Symptoms',
        size: 'md',
        options: [
            { value: 'headache', label: 'Headache' },
            { value: 'weakness', label: 'Weakness' },
            { value: 'cravings', label: 'Cravings' },
            { value: 'good', label: 'Feeling good' },
        ],
        selectedValues: ['good'],
    },
};

export default meta;

type Story = StoryObj<FdUiChipSelectComponent>;

export const Default: Story = {};

export const Small: Story = {
    args: {
        size: 'sm',
        selectedValues: ['headache', 'cravings'],
    },
};
