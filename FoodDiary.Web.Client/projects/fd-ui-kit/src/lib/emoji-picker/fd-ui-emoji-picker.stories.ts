import { signal } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

import { FdUiEmojiPickerComponent } from './fd-ui-emoji-picker.component';

const meta: Meta<FdUiEmojiPickerComponent> = {
    title: 'Components/EmojiPicker',
    component: FdUiEmojiPickerComponent,
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<FdUiEmojiPickerComponent>;

export const Compact: Story = {
    render: () => {
        const selected = signal<number | null>(3);
        return {
            props: {
                selected,
                options: [
                    { value: 1, emoji: '😫', ariaLabel: 'Very low' },
                    { value: 2, emoji: '😟', ariaLabel: 'Low' },
                    { value: 3, emoji: '😐', ariaLabel: 'Neutral' },
                    { value: 4, emoji: '🙂', ariaLabel: 'Good' },
                    { value: 5, emoji: '😌', ariaLabel: 'Excellent' },
                ],
            },
            template: `
                <fd-ui-emoji-picker [options]="options" [(selectedValue)]="selected" ariaLabel="Energy level"></fd-ui-emoji-picker>
                <p style="margin-top: 16px; color: #666;">Selected: {{ selected() }}</p>
            `,
        };
    },
};

export const WithLabels: Story = {
    render: () => {
        const selected = signal<number | null>(2);
        return {
            props: {
                selected,
                options: [
                    { value: 1, emoji: '😣', label: 'Low', description: 'Need rest', ariaLabel: 'Low energy', hint: 'Low energy' },
                    {
                        value: 2,
                        emoji: '🙂',
                        label: 'Stable',
                        description: 'Feeling okay',
                        ariaLabel: 'Stable energy',
                        hint: 'Stable energy',
                    },
                    { value: 3, emoji: '⚡', label: 'High', description: 'Ready to go', ariaLabel: 'High energy', hint: 'High energy' },
                ],
            },
            template: `
                <div style="max-width: 420px;">
                    <fd-ui-emoji-picker
                        [options]="options"
                        [(selectedValue)]="selected"
                        ariaLabel="Mood picker"
                        [showLabels]="true"
                        [showDescriptions]="true"
                        [fullWidth]="true"
                    ></fd-ui-emoji-picker>
                </div>
            `,
        };
    },
};
