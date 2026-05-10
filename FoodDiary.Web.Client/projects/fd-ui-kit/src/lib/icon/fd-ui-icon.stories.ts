import type { Meta, StoryObj } from '@storybook/angular';

import { FdUiIconComponent } from './fd-ui-icon.component';

const meta: Meta<FdUiIconComponent> = {
    title: 'Icon/FdUiIcon',
    component: FdUiIconComponent,
    args: {
        name: 'tune',
        decorative: true,
    },
    argTypes: {
        size: {
            control: 'select',
            options: [null, 'sm', 'md', 'lg', 'xl'],
        },
    },
};

export default meta;

type Story = StoryObj<FdUiIconComponent>;

export const Default: Story = {};

export const Sizes: Story = {
    render: args => ({
        props: args,
        template: `
            <div style="display:flex; gap:var(--fd-space-md); align-items:center;">
                <fd-ui-icon name="tune" size="sm"></fd-ui-icon>
                <fd-ui-icon name="tune" size="md"></fd-ui-icon>
                <fd-ui-icon name="tune" size="lg"></fd-ui-icon>
                <fd-ui-icon name="tune" size="xl"></fd-ui-icon>
            </div>
        `,
    }),
};
