import { Component, input } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';
import { DESIGN_TOKEN_VALUES } from './design-tokens';

type TokenSwatch = { name: string; value: string };
type TokenTableRow = { name: string; value: string };

@Component({
    selector: 'fd-design-tokens-docs',
    standalone: true,
    template: `
        <div
            style="
                height: 100vh;
                overflow: auto;
                box-sizing: border-box;
                padding: 32px 32px 64px;
                font-family: Inter, system-ui, sans-serif;
                color: #1f2937;
                background: #f8fafc;
            "
        >
            <div style="max-width: 1080px;">
                <h1 style="margin: 0 0 16px; font-size: 28px;">{{ pageTitle() }}</h1>
                <p style="margin: 0 0 24px; color: #4b5563; font-size: 15px; line-height: 1.6;">
                    {{ intro() }}
                </p>

                @if (swatchGroups().length > 0) {
                    @for (group of swatchGroups(); track group.title) {
                        <section
                            style="
                                margin-bottom: 24px;
                                padding: 20px;
                                border: 1px solid #e5e7eb;
                                border-radius: 16px;
                                background: #fff;
                            "
                        >
                            <h2 style="margin: 0 0 8px; font-size: 20px;">{{ group.title }}</h2>
                            @if (group.description) {
                                <p style="margin: 0 0 16px; color: #4b5563; line-height: 1.6;">{{ group.description }}</p>
                            }
                            <div style="display: flex; gap: 8px; flex-wrap: wrap;">
                                @for (swatch of group.items; track swatch.name) {
                                    <div style="text-align: center; min-width: 72px;">
                                        <div
                                            [style.background]="swatch.value"
                                            style="height: 56px; border-radius: 10px; border: 1px solid #e5e7eb;"
                                        ></div>
                                        <div style="font-size: 12px; margin-top: 6px; color: #4b5563;">{{ swatch.name }}</div>
                                    </div>
                                }
                            </div>
                        </section>
                    }
                }

                @if (tableRows().length > 0) {
                    <section
                        style="
                            margin-bottom: 24px;
                            padding: 20px;
                            border: 1px solid #e5e7eb;
                            border-radius: 16px;
                            background: #fff;
                        "
                    >
                        <h2 style="margin: 0 0 8px; font-size: 20px;">{{ tableTitle() }}</h2>
                        @if (tableDescription()) {
                            <p style="margin: 0 0 16px; color: #4b5563; line-height: 1.6;">{{ tableDescription() }}</p>
                        }
                        <table style="border-collapse: collapse; width: 100%;">
                            <thead>
                                <tr>
                                    <th style="text-align: left; padding: 10px 8px; border-bottom: 2px solid #e5e7eb;">Token</th>
                                    <th style="text-align: left; padding: 10px 8px; border-bottom: 2px solid #e5e7eb;">Value</th>
                                </tr>
                            </thead>
                            <tbody>
                                @for (row of tableRows(); track row.name) {
                                    <tr>
                                        <td
                                            style="
                                                padding: 10px 8px;
                                                border-bottom: 1px solid #f1f5f9;
                                                font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
                                                font-size: 13px;
                                            "
                                        >
                                            {{ row.name }}
                                        </td>
                                        <td style="padding: 10px 8px; border-bottom: 1px solid #f1f5f9;">{{ row.value }}</td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </section>
                }
            </div>
        </div>
    `,
})
class DesignTokensDocsComponent {
    public readonly pageTitle = input('Design Tokens');
    public readonly intro = input('Reference pages for palette, semantic colors, chart colors, and layout tokens.');
    public readonly swatchGroups = input<Array<{ title: string; description: string | null; items: TokenSwatch[] }>>([]);
    public readonly tableTitle = input('Layout Tokens');
    public readonly tableDescription = input<string | null>(null);
    public readonly tableRows = input<TokenTableRow[]>([]);
}

const primaryShades: TokenSwatch[] = Object.entries(DESIGN_TOKEN_VALUES.color.primary).map(([name, value]) => ({ name, value }));
const secondaryShades: TokenSwatch[] = Object.entries(DESIGN_TOKEN_VALUES.color.secondary).map(([name, value]) => ({ name, value }));
const grayShades: TokenSwatch[] = Object.entries(DESIGN_TOKEN_VALUES.color.gray).map(([name, value]) => ({ name, value }));
const semanticColors: TokenSwatch[] = Object.entries(DESIGN_TOKEN_VALUES.color.semantic).map(([name, value]) => ({ name, value }));
const chartColors: TokenSwatch[] = Object.entries(DESIGN_TOKEN_VALUES.color.chart).map(([name, value]) => ({ name, value }));

const layoutTokens: TokenTableRow[] = [
    { name: 'page.background', value: DESIGN_TOKEN_VALUES.layout.page.background },
    { name: 'page.horizontalPadding', value: DESIGN_TOKEN_VALUES.layout.page.horizontalPadding },
    { name: 'page.verticalPadding', value: DESIGN_TOKEN_VALUES.layout.page.verticalPadding },
    { name: 'page.contentMaxWidth', value: DESIGN_TOKEN_VALUES.layout.page.contentMaxWidth },
    { name: 'page.sectionSpacing', value: DESIGN_TOKEN_VALUES.layout.page.sectionSpacing },
    { name: 'header.height', value: DESIGN_TOKEN_VALUES.layout.header.height },
    { name: 'header.background', value: DESIGN_TOKEN_VALUES.layout.header.background },
    { name: 'header.textColor', value: DESIGN_TOKEN_VALUES.layout.header.textColor },
];

const meta: Meta<DesignTokensDocsComponent> = {
    title: 'Foundation/Design Tokens',
    component: DesignTokensDocsComponent,
    tags: ['autodocs'],
    parameters: {
        layout: 'fullscreen',
        controls: {
            disable: true,
        },
    },
};

export default meta;
type Story = StoryObj<DesignTokensDocsComponent>;

export const Overview: Story = {
    args: {
        pageTitle: 'Design Tokens Overview',
        intro: 'High-level reference for the shared palette and layout primitives. Use the sub-pages in this section for focused browsing.',
        swatchGroups: [
            {
                title: 'Core Palette',
                description: 'Primary, secondary, and gray ramps used across product surfaces, text, and emphasis.',
                items: [...primaryShades.slice(0, 5), ...secondaryShades.slice(0, 5), ...grayShades.slice(0, 5)],
            },
            {
                title: 'Semantic Colors',
                description: 'Status and meaning-driven colors.',
                items: semanticColors,
            },
            {
                title: 'Chart Colors',
                description: 'Default data visualization colors.',
                items: chartColors,
            },
        ],
        tableTitle: 'Layout Tokens',
        tableDescription: 'Spacing and shell tokens used across pages.',
        tableRows: layoutTokens,
    },
};

export const Colors: Story = {
    args: {
        pageTitle: 'Core Colors',
        intro: 'Primary, secondary, and neutral ramps used in the main interface.',
        swatchGroups: [
            { title: 'Primary', description: null, items: primaryShades },
            { title: 'Secondary', description: null, items: secondaryShades },
            { title: 'Gray', description: null, items: grayShades },
        ],
        tableRows: [],
    },
};

export const Semantic: Story = {
    args: {
        pageTitle: 'Semantic Colors',
        intro: 'Meaning-driven colors for success, warning, danger, and info states.',
        swatchGroups: [
            {
                title: 'Semantic',
                description: 'Use these for state communication, not for arbitrary decoration.',
                items: semanticColors,
            },
        ],
        tableRows: [],
    },
};

export const Charts: Story = {
    args: {
        pageTitle: 'Chart Colors',
        intro: 'Default palette for nutrition and analytics visualizations.',
        swatchGroups: [
            {
                title: 'Charts',
                description: 'Keep chart color meaning stable across related visualizations.',
                items: chartColors,
            },
        ],
        tableRows: [],
    },
};

export const Layout: Story = {
    args: {
        pageTitle: 'Layout Tokens',
        intro: 'Shared page shell and spacing tokens used by the client layout system.',
        swatchGroups: [],
        tableTitle: 'Layout Tokens',
        tableDescription: 'Spacing and structure values for page composition.',
        tableRows: layoutTokens,
    },
};
