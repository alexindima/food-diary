import { FormsModule } from '@angular/forms';
import type { Meta, StoryObj } from '@storybook/angular';
import { moduleMetadata } from '@storybook/angular';

import { FdUiSatietyScaleComponent } from './fd-ui-satiety-scale.component';

const meta: Meta<FdUiSatietyScaleComponent> = {
    title: 'Components/SatietyScale',
    component: FdUiSatietyScaleComponent,
    tags: ['autodocs'],
    decorators: [moduleMetadata({ imports: [FormsModule] })],
    argTypes: {
        label: { control: 'text' },
        hint: { control: 'text' },
        error: { control: 'text' },
        required: { control: 'boolean' },
        layout: { control: 'select', options: ['grid', 'vertical'] },
    },
};

export default meta;
type Story = StoryObj<FdUiSatietyScaleComponent>;

export const Grid: Story = {
    args: {
        label: 'How hungry are you?',
        layout: 'grid',
    },
};

export const Vertical: Story = {
    args: {
        label: 'Satiety Level',
        layout: 'vertical',
    },
};

export const WithHint: Story = {
    args: {
        label: 'How full do you feel?',
        hint: 'Rate from 0 (starving) to 9 (overly full)',
        layout: 'grid',
    },
};

export const WithError: Story = {
    args: {
        label: 'Satiety',
        error: 'Please select your satiety level',
        layout: 'grid',
    },
};

export const Required: Story = {
    args: {
        label: 'Hunger Level',
        required: true,
        layout: 'grid',
    },
};

export const WithValue: Story = {
    render: () => ({
        template: '<fd-ui-satiety-scale label="Current level" layout="grid" [ngModel]="5"></fd-ui-satiety-scale>',
    }),
};
