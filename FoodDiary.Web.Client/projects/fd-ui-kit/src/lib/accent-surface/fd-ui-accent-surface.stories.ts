import type { Meta, StoryObj } from '@storybook/angular';
import { FdUiAccentSurfaceComponent } from './fd-ui-accent-surface.component';

const meta: Meta<FdUiAccentSurfaceComponent> = {
    title: 'Components/AccentSurface',
    component: FdUiAccentSurfaceComponent,
    tags: ['autodocs'],
    argTypes: {
        accentSide: {
            control: 'select',
            options: ['top', 'right', 'bottom', 'left'],
        },
        accentColor: { control: 'color' },
        active: { control: 'boolean' },
        tinted: { control: 'boolean' },
    },
    render: args => ({
        props: args,
        template: `
            <fd-ui-accent-surface
                [accentSide]="accentSide"
                [accentColor]="accentColor"
                [active]="active"
                [tinted]="tinted"
                style="display: block; padding: 24px; border-radius: 8px; background: white;">
                <p style="margin: 0;">Accent surface content</p>
            </fd-ui-accent-surface>
        `,
    }),
};

export default meta;
type Story = StoryObj<FdUiAccentSurfaceComponent>;

export const Top: Story = {
    args: {
        accentSide: 'top',
        accentColor: '#2563eb',
        active: true,
    },
};

export const Left: Story = {
    args: {
        accentSide: 'left',
        accentColor: '#2563eb',
        active: true,
    },
};

export const Right: Story = {
    args: {
        accentSide: 'right',
        accentColor: '#f5a623',
        active: true,
    },
};

export const Bottom: Story = {
    args: {
        accentSide: 'bottom',
        accentColor: '#50e3c2',
        active: true,
    },
};

export const Tinted: Story = {
    args: {
        accentSide: 'left',
        accentColor: '#4a90e2',
        active: true,
        tinted: true,
    },
};

export const Inactive: Story = {
    args: {
        accentSide: 'top',
        accentColor: '#2563eb',
        active: false,
    },
};

export const AllSides: Story = {
    render: () => ({
        template: `
            <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 16px; max-width: 600px;">
                <fd-ui-accent-surface accentSide="top" accentColor="#2563eb" [active]="true" style="display:block;padding:24px;border-radius:8px;background:white;">
                    <p style="margin:0">Top accent</p>
                </fd-ui-accent-surface>
                <fd-ui-accent-surface accentSide="right" accentColor="#f5a623" [active]="true" style="display:block;padding:24px;border-radius:8px;background:white;">
                    <p style="margin:0">Right accent</p>
                </fd-ui-accent-surface>
                <fd-ui-accent-surface accentSide="bottom" accentColor="#50e3c2" [active]="true" style="display:block;padding:24px;border-radius:8px;background:white;">
                    <p style="margin:0">Bottom accent</p>
                </fd-ui-accent-surface>
                <fd-ui-accent-surface accentSide="left" accentColor="#ff6b6b" [active]="true" style="display:block;padding:24px;border-radius:8px;background:white;">
                    <p style="margin:0">Left accent</p>
                </fd-ui-accent-surface>
            </div>
        `,
    }),
};
