import type { Meta, StoryObj } from '@storybook/angular-vite';
import { moduleMetadata } from '@storybook/angular-vite';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiCardComponent } from '../card/fd-ui-card';
import { FdUiTopLoaderComponent } from '../top-loader/fd-ui-top-loader';
import { FdUiLoaderComponent } from './fd-ui-loader';

const meta: Meta<FdUiLoaderComponent> = {
    title: 'Components/Loader',
    component: FdUiLoaderComponent,
    tags: ['autodocs'],
    decorators: [moduleMetadata({ imports: [FdUiCardComponent, FdUiButtonComponent, FdUiTopLoaderComponent] })],
};

export default meta;
type Story = StoryObj<FdUiLoaderComponent>;

export const Default: Story = {};

export const LoadingFeedback: Story = {
    render: () => ({
        template: `
            <div class="fd-stack fd-gap-md">
                <fd-ui-top-loader [visible]="true" />
                <fd-ui-loader />
                <fd-ui-button [loading]="true">Save changes</fd-ui-button>
                <p role="status">Saving changes…</p>
            </div>
        `,
    }),
};

export const InContext: Story = {
    render: () => ({
        template: `
            <div style="display: flex; flex-direction: column; align-items: center; gap: var(--fd-space-md); padding: var(--fd-space-xxl); background: #f5f5f5; border-radius: var(--fd-radius-md);">
                <fd-ui-loader></fd-ui-loader>
                <p style="margin: 0; color: #666; font-size: var(--fd-text-caption-size);">Loading data...</p>
            </div>
        `,
    }),
};

export const InCard: Story = {
    render: () => ({
        template: `
            <fd-ui-card title="Daily Summary">
                <div style="display: flex; justify-content: center; padding: var(--fd-space-xl);">
                    <fd-ui-loader></fd-ui-loader>
                </div>
            </fd-ui-card>
        `,
    }),
};
