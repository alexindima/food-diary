import type { Meta, StoryObj } from '@storybook/angular';

import { FdUiButtonComponent } from './fd-ui-button.component';

const meta: Meta<FdUiButtonComponent> = {
    title: 'Components/Button',
    component: FdUiButtonComponent,
    tags: ['autodocs'],
    argTypes: {
        variant: {
            control: 'select',
            options: ['primary', 'secondary', 'danger', 'info', 'ghost', 'outline'],
            description: 'Visual style variant',
        },
        fill: {
            control: 'select',
            options: ['solid', 'outline', 'text', 'ghost'],
            description: 'Fill style of the button',
        },
        size: {
            control: 'select',
            options: ['xs', 'sm', 'md', 'lg'],
            description: 'Button size',
        },
        type: {
            control: 'select',
            options: ['button', 'submit', 'reset'],
        },
        icon: {
            control: 'text',
            description: 'Material Icon name',
        },
        iconSize: {
            control: 'select',
            options: ['xs', 'sm', 'md', 'lg', 'xl'],
        },
        disabled: { control: 'boolean' },
        fullWidth: { control: 'boolean' },
        ariaLabel: { control: 'text' },
    },
    render: args => ({
        props: args,
        template: `<fd-ui-button
            [variant]="variant"
            [fill]="fill"
            [size]="size"
            [icon]="icon"
            [iconSize]="iconSize"
            [disabled]="disabled"
            [fullWidth]="fullWidth"
            [ariaLabel]="ariaLabel"
            [type]="type">Button</fd-ui-button>`,
    }),
};

export default meta;
type Story = StoryObj<FdUiButtonComponent>;

export const Primary: Story = {
    args: {
        variant: 'primary',
        fill: 'solid',
        size: 'md',
    },
};

export const Secondary: Story = {
    args: {
        variant: 'secondary',
        fill: 'solid',
        size: 'md',
    },
};

export const Danger: Story = {
    args: {
        variant: 'danger',
        fill: 'solid',
        size: 'md',
    },
};

export const Info: Story = {
    args: {
        variant: 'info',
        fill: 'solid',
        size: 'md',
    },
};

export const Ghost: Story = {
    args: {
        variant: 'ghost',
        size: 'md',
    },
};

export const Outline: Story = {
    args: {
        variant: 'outline',
        size: 'md',
    },
};

export const WithIcon: Story = {
    args: {
        variant: 'primary',
        fill: 'solid',
        size: 'md',
        icon: 'add',
    },
};

export const IconOnly: Story = {
    args: {
        variant: 'primary',
        fill: 'solid',
        size: 'md',
        icon: 'edit',
        ariaLabel: 'Edit',
    },
    render: args => ({
        props: args,
        template: `<fd-ui-button
            [variant]="variant"
            [fill]="fill"
            [size]="size"
            [icon]="icon"
            [ariaLabel]="ariaLabel"
            [type]="type"></fd-ui-button>`,
    }),
};

export const Small: Story = {
    args: { variant: 'primary', fill: 'solid', size: 'sm' },
};

export const Large: Story = {
    args: { variant: 'primary', fill: 'solid', size: 'lg' },
};

export const ExtraSmall: Story = {
    args: { variant: 'primary', fill: 'solid', size: 'xs' },
};

export const FullWidth: Story = {
    args: {
        variant: 'primary',
        fill: 'solid',
        size: 'md',
        fullWidth: true,
    },
};

export const Disabled: Story = {
    args: {
        variant: 'primary',
        fill: 'solid',
        size: 'md',
        disabled: true,
    },
};

export const AllVariants: Story = {
    render: () => ({
        template: `
            <div style="display: flex; gap: var(--fd-space-sm); flex-wrap: wrap; align-items: center;">
                <fd-ui-button variant="primary" fill="solid">Primary</fd-ui-button>
                <fd-ui-button variant="secondary" fill="solid">Secondary</fd-ui-button>
                <fd-ui-button variant="danger" fill="solid">Danger</fd-ui-button>
                <fd-ui-button variant="info" fill="solid">Info</fd-ui-button>
                <fd-ui-button variant="ghost">Ghost</fd-ui-button>
                <fd-ui-button variant="outline">Outline</fd-ui-button>
            </div>
        `,
    }),
};

export const AllSizes: Story = {
    render: () => ({
        template: `
            <div style="display: flex; gap: var(--fd-space-sm); align-items: center;">
                <fd-ui-button variant="primary" size="xs">XS</fd-ui-button>
                <fd-ui-button variant="primary" size="sm">SM</fd-ui-button>
                <fd-ui-button variant="primary" size="md">MD</fd-ui-button>
                <fd-ui-button variant="primary" size="lg">LG</fd-ui-button>
            </div>
        `,
    }),
};

export const AllFills: Story = {
    render: () => ({
        template: `
            <div style="display: flex; gap: var(--fd-space-sm); align-items: center;">
                <fd-ui-button variant="primary" fill="solid">Solid</fd-ui-button>
                <fd-ui-button variant="primary" fill="outline">Outline</fd-ui-button>
                <fd-ui-button variant="primary" fill="text">Text</fd-ui-button>
                <fd-ui-button variant="primary" fill="ghost">Ghost</fd-ui-button>
            </div>
        `,
    }),
};
