import type { Meta, StoryObj } from '@storybook/angular';
import { FdUiEntityCardComponent } from './fd-ui-entity-card.component';

const meta: Meta<FdUiEntityCardComponent> = {
    title: 'Components/EntityCard',
    component: FdUiEntityCardComponent,
    tags: ['autodocs'],
    argTypes: {
        title: { control: 'text' },
        meta: { control: 'text' },
        imageUrl: { control: 'text' },
        fallbackImage: { control: 'text' },
        appearance: {
            control: 'select',
            options: ['default', 'product', 'recipe', 'info', 'general', 'entry'],
        },
    },
    render: args => ({
        props: args,
        template: `
            <fd-ui-entity-card [title]="title" [meta]="meta" [imageUrl]="imageUrl" [appearance]="appearance">
                <p style="margin: 0; color: #666;">Entity card content</p>
            </fd-ui-entity-card>
        `,
    }),
};

export default meta;
type Story = StoryObj<FdUiEntityCardComponent>;

export const Default: Story = {
    args: {
        title: 'Chicken Breast',
        meta: '165 kcal / 100g',
        appearance: 'product',
    },
};

export const WithImage: Story = {
    args: {
        title: 'Greek Salad',
        meta: '120 kcal / serving',
        imageUrl: 'https://placehold.co/96x96/50e3c2/ffffff?text=GS',
        appearance: 'recipe',
    },
};

export const WithFallback: Story = {
    args: {
        title: 'Unknown Product',
        meta: 'No image available',
        imageUrl: null,
        appearance: 'product',
    },
};

export const MultipleCards: Story = {
    render: () => ({
        template: `
            <div style="display: flex; flex-direction: column; gap: 12px; max-width: 500px;">
                <fd-ui-entity-card title="Oatmeal" meta="150 kcal" appearance="product">
                    <p style="margin: 0; font-size: 14px; color: #666;">P: 5g | F: 3g | C: 27g</p>
                </fd-ui-entity-card>
                <fd-ui-entity-card title="Banana" meta="89 kcal" appearance="product">
                    <p style="margin: 0; font-size: 14px; color: #666;">P: 1g | F: 0g | C: 23g</p>
                </fd-ui-entity-card>
                <fd-ui-entity-card title="Greek Yogurt" meta="100 kcal" appearance="product">
                    <p style="margin: 0; font-size: 14px; color: #666;">P: 17g | F: 1g | C: 6g</p>
                </fd-ui-entity-card>
            </div>
        `,
    }),
};
