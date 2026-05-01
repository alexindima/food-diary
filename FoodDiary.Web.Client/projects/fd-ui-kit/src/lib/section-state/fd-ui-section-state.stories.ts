import type { Meta, StoryObj } from '@storybook/angular';

import { FdUiSectionStateComponent } from './fd-ui-section-state.component';

const meta: Meta<FdUiSectionStateComponent> = {
    title: 'Components/SectionState',
    component: FdUiSectionStateComponent,
    tags: ['autodocs'],
    argTypes: {
        state: {
            control: 'select',
            options: ['content', 'loading', 'empty', 'error'],
        },
        appearance: {
            control: 'select',
            options: ['default', 'compact'],
        },
    },
    render: args => ({
        props: args,
        template: `
            <div style="max-width: 560px; margin: 0 auto;">
                <fd-ui-section-state
                    [state]="state"
                    [appearance]="appearance"
                    [loadingLabel]="loadingLabel"
                    [emptyTitle]="emptyTitle"
                    [emptyMessage]="emptyMessage"
                    [emptyIcon]="emptyIcon"
                    [errorTitle]="errorTitle"
                    [errorMessage]="errorMessage"
                    [retryLabel]="retryLabel"
                >
                    <div style="padding: var(--fd-space-lg); border: 1px dashed var(--fd-color-border); border-radius: var(--fd-radius-card);">
                        Real section content goes here.
                    </div>
                </fd-ui-section-state>
            </div>
        `,
    }),
};

export default meta;
type Story = StoryObj<FdUiSectionStateComponent>;

export const Loading: Story = {
    args: {
        state: 'loading',
        appearance: 'default',
        loadingLabel: 'Loading section data...',
        emptyTitle: null,
        emptyMessage: 'Nothing here yet.',
        emptyIcon: 'inventory_2',
        errorTitle: 'Unable to load this section',
        errorMessage: 'Try again in a moment.',
        retryLabel: 'Retry',
    },
};

export const Empty: Story = {
    args: {
        ...Loading.args,
        state: 'empty',
        emptyTitle: 'No body metrics yet',
        emptyMessage: 'Add weight or waist entries to start seeing trends here.',
        emptyIcon: 'monitoring',
    },
};

export const Error: Story = {
    args: {
        ...Loading.args,
        state: 'error',
        errorTitle: 'Section failed to load',
        errorMessage: 'The request did not complete. You can retry without leaving the page.',
    },
};

export const Content: Story = {
    args: {
        ...Loading.args,
        state: 'content',
    },
};
