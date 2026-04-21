import type { Meta, StoryObj } from '@storybook/angular';
import { FdUiStatusBadgeComponent } from './fd-ui-status-badge.component';

const meta: Meta<FdUiStatusBadgeComponent> = {
    title: 'Components/StatusBadge',
    component: FdUiStatusBadgeComponent,
    tags: ['autodocs'],
    argTypes: {
        tone: {
            control: 'select',
            options: ['muted', 'success', 'warning', 'danger'],
        },
    },
    render: args => ({
        props: args,
        template: '<fd-ui-status-badge [tone]="tone">Changes saved</fd-ui-status-badge>',
    }),
};

export default meta;
type Story = StoryObj<FdUiStatusBadgeComponent>;

export const Default: Story = {
    args: {
        tone: 'muted',
    },
};

export const AllTones: Story = {
    render: () => ({
        template: `
            <div style="display:flex; gap:12px; flex-wrap:wrap; align-items:center;">
                <fd-ui-status-badge tone="muted">Syncing</fd-ui-status-badge>
                <fd-ui-status-badge tone="success">Saved</fd-ui-status-badge>
                <fd-ui-status-badge tone="warning">Unsaved changes</fd-ui-status-badge>
                <fd-ui-status-badge tone="danger">Save failed</fd-ui-status-badge>
            </div>
        `,
    }),
};
