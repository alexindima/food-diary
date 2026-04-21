import type { Meta, StoryObj } from '@storybook/angular';
import { FdUiEmptyStateComponent } from './fd-ui-empty-state.component';

const meta: Meta<FdUiEmptyStateComponent> = {
    title: 'Components/EmptyState',
    component: FdUiEmptyStateComponent,
    tags: ['autodocs'],
    argTypes: {
        appearance: {
            control: 'select',
            options: ['default', 'compact'],
        },
        title: { control: 'text' },
        message: { control: 'text' },
        icon: { control: 'text' },
    },
};

export default meta;
type Story = StoryObj<FdUiEmptyStateComponent>;

export const Default: Story = {
    args: {
        title: 'Nothing here yet',
        message: 'Start by adding your first item to replace this placeholder.',
        icon: 'inventory_2',
        appearance: 'default',
    },
};

export const Compact: Story = {
    args: {
        title: 'No connected devices',
        message: 'This account does not have active web push subscriptions yet.',
        icon: 'devices',
        appearance: 'compact',
    },
};
