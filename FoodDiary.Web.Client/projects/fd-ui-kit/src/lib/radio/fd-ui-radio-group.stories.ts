import type { Meta, StoryObj } from '@storybook/angular';
import { FormsModule } from '@angular/forms';
import { moduleMetadata } from '@storybook/angular';
import { FdUiRadioGroupComponent } from './fd-ui-radio-group.component';

const meta: Meta<FdUiRadioGroupComponent<string>> = {
    title: 'Components/RadioGroup',
    component: FdUiRadioGroupComponent,
    tags: ['autodocs'],
    decorators: [moduleMetadata({ imports: [FormsModule] })],
    argTypes: {
        label: { control: 'text' },
        hint: { control: 'text' },
        error: { control: 'text' },
        required: { control: 'boolean' },
        orientation: { control: 'select', options: ['vertical', 'horizontal'] },
    },
};

export default meta;
type Story = StoryObj<FdUiRadioGroupComponent<string>>;

const goalOptions = [
    { value: 'lose', label: 'Lose weight' },
    { value: 'maintain', label: 'Maintain weight' },
    { value: 'gain', label: 'Gain weight' },
];

export const Vertical: Story = {
    args: {
        label: 'Your Goal',
        options: goalOptions,
        orientation: 'vertical',
    },
};

export const Horizontal: Story = {
    args: {
        label: 'Your Goal',
        options: goalOptions,
        orientation: 'horizontal',
    },
};

export const WithDescriptions: Story = {
    args: {
        label: 'Activity Level',
        options: [
            { value: 'sedentary', label: 'Sedentary', description: 'Desk job, minimal exercise' },
            { value: 'moderate', label: 'Moderate', description: '3-5 workouts per week' },
            { value: 'active', label: 'Active', description: 'Daily exercise or physical job' },
        ],
        orientation: 'vertical',
    },
};

export const WithHint: Story = {
    args: {
        label: 'Diet Type',
        hint: 'This affects your macro calculation',
        options: [
            { value: 'balanced', label: 'Balanced' },
            { value: 'keto', label: 'Keto' },
            { value: 'vegan', label: 'Vegan' },
        ],
        orientation: 'vertical',
    },
};

export const WithError: Story = {
    args: {
        label: 'Gender',
        error: 'Please select an option',
        options: [
            { value: 'male', label: 'Male' },
            { value: 'female', label: 'Female' },
        ],
        orientation: 'horizontal',
    },
};

export const Required: Story = {
    args: {
        label: 'Goal',
        required: true,
        options: goalOptions,
        orientation: 'vertical',
    },
};
