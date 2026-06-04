import type { Meta, StoryObj } from '@storybook/angular';

import { FdUiCheckboxComponent } from './fd-ui-checkbox';

const meta: Meta<FdUiCheckboxComponent> = {
    title: 'Components/Checkbox',
    component: FdUiCheckboxComponent,
    tags: ['autodocs'],
    argTypes: {
        label: { control: 'text' },
        hint: { control: 'text' },
    },
};

export default meta;
type Story = StoryObj<FdUiCheckboxComponent>;

export const Default: Story = {
    args: {
        label: 'Accept terms and conditions',
    },
};

export const WithHint: Story = {
    args: {
        label: 'Enable notifications',
        hint: 'You will receive daily nutrition reminders',
    },
};

export const Checked: Story = {
    render: () => ({
        template: '<fd-ui-checkbox label="Checked by default" [checked]="true"></fd-ui-checkbox>',
    }),
};

export const Multiple: Story = {
    render: () => ({
        template: `
            <div style="display: flex; flex-direction: column; gap: var(--fd-space-xs);">
                <fd-ui-checkbox label="Proteins" [checked]="true"></fd-ui-checkbox>
                <fd-ui-checkbox label="Fats"></fd-ui-checkbox>
                <fd-ui-checkbox label="Carbohydrates" [checked]="true"></fd-ui-checkbox>
                <fd-ui-checkbox label="Fiber"></fd-ui-checkbox>
            </div>
        `,
    }),
};
