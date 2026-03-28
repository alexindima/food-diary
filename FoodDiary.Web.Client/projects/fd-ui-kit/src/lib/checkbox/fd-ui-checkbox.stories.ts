import type { Meta, StoryObj } from '@storybook/angular';
import { FormsModule } from '@angular/forms';
import { moduleMetadata } from '@storybook/angular';
import { FdUiCheckboxComponent } from './fd-ui-checkbox.component';

const meta: Meta<FdUiCheckboxComponent> = {
    title: 'Components/Checkbox',
    component: FdUiCheckboxComponent,
    tags: ['autodocs'],
    decorators: [moduleMetadata({ imports: [FormsModule] })],
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
        template: '<fd-ui-checkbox label="Checked by default" [ngModel]="true"></fd-ui-checkbox>',
    }),
};

export const Multiple: Story = {
    render: () => ({
        template: `
            <div style="display: flex; flex-direction: column; gap: 8px;">
                <fd-ui-checkbox label="Proteins" [ngModel]="true"></fd-ui-checkbox>
                <fd-ui-checkbox label="Fats"></fd-ui-checkbox>
                <fd-ui-checkbox label="Carbohydrates" [ngModel]="true"></fd-ui-checkbox>
                <fd-ui-checkbox label="Fiber"></fd-ui-checkbox>
            </div>
        `,
    }),
};
