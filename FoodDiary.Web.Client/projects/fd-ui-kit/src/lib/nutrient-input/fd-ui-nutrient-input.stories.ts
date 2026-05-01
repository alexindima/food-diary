import { FormsModule } from '@angular/forms';
import type { Meta, StoryObj } from '@storybook/angular';
import { moduleMetadata } from '@storybook/angular';

import { FdUiNutrientInputComponent } from './fd-ui-nutrient-input.component';

const meta: Meta<FdUiNutrientInputComponent> = {
    title: 'Components/NutrientInput',
    component: FdUiNutrientInputComponent,
    tags: ['autodocs'],
    decorators: [moduleMetadata({ imports: [FormsModule] })],
    argTypes: {
        label: { control: 'text' },
        icon: { control: 'text' },
        placeholder: { control: 'text' },
        type: { control: 'select', options: ['text', 'number'] },
        size: { control: 'select', options: ['sm', 'md', 'lg'] },
        variant: { control: 'select', options: ['tinted', 'outline'] },
        tintColor: { control: 'color' },
        textColor: { control: 'color' },
        unitLabel: { control: 'text' },
        valueAlign: { control: 'select', options: ['center', 'left'] },
        labelUppercase: { control: 'boolean' },
        required: { control: 'boolean' },
        readonly: { control: 'boolean' },
        error: { control: 'text' },
        step: { control: 'text' },
        min: { control: 'text' },
        max: { control: 'text' },
    },
};

export default meta;
type Story = StoryObj<FdUiNutrientInputComponent>;

export const Default: Story = {
    args: {
        label: 'Calories',
        placeholder: '0',
        type: 'number',
        size: 'md',
        variant: 'tinted',
    },
};

export const Calories: Story = {
    args: {
        label: 'Calories',
        icon: 'local_fire_department',
        tintColor: '#ff6b35',
        textColor: '#ff6b35',
        unitLabel: 'kcal',
        variant: 'tinted',
        size: 'md',
    },
};

export const Proteins: Story = {
    args: {
        label: 'Proteins',
        icon: 'fitness_center',
        tintColor: '#4a90e2',
        textColor: '#4a90e2',
        unitLabel: 'g',
        variant: 'tinted',
        size: 'md',
    },
};

export const Fats: Story = {
    args: {
        label: 'Fats',
        icon: 'water_drop',
        tintColor: '#f5a623',
        textColor: '#f5a623',
        unitLabel: 'g',
        variant: 'tinted',
        size: 'md',
    },
};

export const Carbs: Story = {
    args: {
        label: 'Carbs',
        icon: 'grain',
        tintColor: '#50e3c2',
        textColor: '#50e3c2',
        unitLabel: 'g',
        variant: 'tinted',
        size: 'md',
    },
};

export const OutlineVariant: Story = {
    args: {
        label: 'Weight',
        unitLabel: 'g',
        variant: 'outline',
        size: 'md',
    },
};

export const Small: Story = {
    args: {
        label: 'Value',
        variant: 'tinted',
        size: 'sm',
    },
};

export const Large: Story = {
    args: {
        label: 'Value',
        variant: 'tinted',
        size: 'lg',
    },
};

export const WithError: Story = {
    args: {
        label: 'Calories',
        error: 'Value is required',
        variant: 'tinted',
        size: 'md',
    },
};

export const Readonly: Story = {
    render: () => ({
        template:
            '<fd-ui-nutrient-input label="Total" [ngModel]="350" unitLabel="kcal" [readonly]="true" tintColor="#ff6b35" textColor="#ff6b35"></fd-ui-nutrient-input>',
    }),
};

export const NutritionGroup: Story = {
    render: () => ({
        template: `
            <div style="display: flex; gap: var(--fd-space-sm); flex-wrap: wrap;">
                <fd-ui-nutrient-input label="Calories" icon="local_fire_department" [ngModel]="2100" unitLabel="kcal" tintColor="#ff6b35" textColor="#ff6b35"></fd-ui-nutrient-input>
                <fd-ui-nutrient-input label="Proteins" icon="fitness_center" [ngModel]="85" unitLabel="g" tintColor="#4a90e2" textColor="#4a90e2"></fd-ui-nutrient-input>
                <fd-ui-nutrient-input label="Fats" icon="water_drop" [ngModel]="70" unitLabel="g" tintColor="#f5a623" textColor="#f5a623"></fd-ui-nutrient-input>
                <fd-ui-nutrient-input label="Carbs" icon="grain" [ngModel]="250" unitLabel="g" tintColor="#50e3c2" textColor="#50e3c2"></fd-ui-nutrient-input>
            </div>
        `,
    }),
};
