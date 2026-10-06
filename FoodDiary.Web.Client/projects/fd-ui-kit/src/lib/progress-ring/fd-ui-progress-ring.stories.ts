import type { Meta, StoryObj } from '@storybook/angular-vite';

import { FdUiProgressRingComponent } from './fd-ui-progress-ring';

const meta: Meta<FdUiProgressRingComponent> = {
    title: 'Components/Progress Ring',
    component: FdUiProgressRingComponent,
    args: {
        value: 68,
        max: 100,
        ariaLabel: '68 percent complete',
    },
};

export default meta;
type Story = StoryObj<FdUiProgressRingComponent>;

export const Default: Story = {};
