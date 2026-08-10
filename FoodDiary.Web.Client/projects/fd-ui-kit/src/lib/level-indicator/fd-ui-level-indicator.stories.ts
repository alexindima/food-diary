import type { Meta, StoryObj } from '@storybook/angular';

import { FdUiLevelIndicatorComponent } from './fd-ui-level-indicator';

const meta: Meta<FdUiLevelIndicatorComponent> = {
    title: 'Components/Level Indicator',
    component: FdUiLevelIndicatorComponent,
    args: {
        filledCount: 2,
    },
};

export default meta;
type Story = StoryObj<FdUiLevelIndicatorComponent>;

export const Default: Story = {};
