import type { Meta, StoryObj } from '@storybook/angular';
import { FdUiCardComponent } from './fd-ui-card.component';

const meta: Meta<FdUiCardComponent> = {
    title: 'Components/Card',
    component: FdUiCardComponent,
    tags: ['autodocs'],
    argTypes: {
        title: { control: 'text' },
        meta: { control: 'text' },
        subtle: { control: 'boolean' },
        appearance: {
            control: 'select',
            options: ['default', 'product', 'recipe', 'info', 'general', 'entry'],
        },
    },
    render: (args) => ({
        props: args,
        template: `
            <fd-ui-card [title]="title" [meta]="meta" [subtle]="subtle" [appearance]="appearance">
                <p style="margin: 0; color: #666;">Card content goes here. This is a flexible container for any content.</p>
            </fd-ui-card>
        `,
    }),
};

export default meta;
type Story = StoryObj<FdUiCardComponent>;

export const Default: Story = {
    args: {
        title: 'Daily Summary',
        meta: 'Today',
        appearance: 'default',
    },
};

export const Product: Story = {
    args: {
        title: 'Chicken Breast',
        meta: '165 kcal / 100g',
        appearance: 'product',
    },
};

export const Recipe: Story = {
    args: {
        title: 'Greek Salad',
        meta: '4 ingredients',
        appearance: 'recipe',
    },
};

export const Info: Story = {
    args: {
        title: 'Nutrition Tip',
        appearance: 'info',
    },
};

export const Entry: Story = {
    args: {
        title: 'Weight Entry',
        meta: '75.5 kg',
        appearance: 'entry',
    },
};

export const Subtle: Story = {
    args: {
        title: 'Subtle Card',
        meta: 'Less prominent',
        subtle: true,
    },
};

export const WithoutTitle: Story = {
    render: () => ({
        template: `
            <fd-ui-card>
                <p style="margin: 0;">A card without a title, just content.</p>
            </fd-ui-card>
        `,
    }),
};

export const WithActions: Story = {
    render: () => ({
        template: `
            <fd-ui-card title="Meal Log" meta="March 29">
                <ng-container fdUiCardActions>
                    <fd-ui-button variant="ghost" size="xs" icon="more_vert" ariaLabel="More options"></fd-ui-button>
                </ng-container>
                <p style="margin: 0; color: #666;">Breakfast: 450 kcal</p>
            </fd-ui-card>
        `,
    }),
};

export const AllAppearances: Story = {
    render: () => ({
        template: `
            <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 16px;">
                <fd-ui-card title="Default" appearance="default"><p style="margin:0;color:#666">Default appearance</p></fd-ui-card>
                <fd-ui-card title="Product" appearance="product"><p style="margin:0;color:#666">Product appearance</p></fd-ui-card>
                <fd-ui-card title="Recipe" appearance="recipe"><p style="margin:0;color:#666">Recipe appearance</p></fd-ui-card>
                <fd-ui-card title="Info" appearance="info"><p style="margin:0;color:#666">Info appearance</p></fd-ui-card>
                <fd-ui-card title="General" appearance="general"><p style="margin:0;color:#666">General appearance</p></fd-ui-card>
                <fd-ui-card title="Entry" appearance="entry"><p style="margin:0;color:#666">Entry appearance</p></fd-ui-card>
            </div>
        `,
    }),
};
