import type { Meta, StoryObj } from '@storybook/angular';
import { Component } from '@angular/core';
import { DESIGN_TOKEN_VALUES } from './design-tokens';

@Component({
    selector: 'fd-design-tokens-docs',
    standalone: true,
    template: `
        <div style="font-family: Inter, system-ui, sans-serif;">
            <h2 style="margin-top: 0;">Color Palette</h2>

            <h3>Primary</h3>
            <div style="display: flex; gap: 4px; flex-wrap: wrap;">
                @for (shade of primaryShades; track shade.name) {
                    <div style="text-align: center;">
                        <div
                            [style.background]="shade.value"
                            style="width: 64px; height: 48px; border-radius: 6px; border: 1px solid #e0e0e0;"
                        ></div>
                        <div style="font-size: 11px; margin-top: 4px; color: #666;">{{ shade.name }}</div>
                    </div>
                }
            </div>

            <h3>Secondary</h3>
            <div style="display: flex; gap: 4px; flex-wrap: wrap;">
                @for (shade of secondaryShades; track shade.name) {
                    <div style="text-align: center;">
                        <div
                            [style.background]="shade.value"
                            style="width: 64px; height: 48px; border-radius: 6px; border: 1px solid #e0e0e0;"
                        ></div>
                        <div style="font-size: 11px; margin-top: 4px; color: #666;">{{ shade.name }}</div>
                    </div>
                }
            </div>

            <h3>Gray</h3>
            <div style="display: flex; gap: 4px; flex-wrap: wrap;">
                @for (shade of grayShades; track shade.name) {
                    <div style="text-align: center;">
                        <div
                            [style.background]="shade.value"
                            style="width: 64px; height: 48px; border-radius: 6px; border: 1px solid #e0e0e0;"
                        ></div>
                        <div style="font-size: 11px; margin-top: 4px; color: #666;">{{ shade.name }}</div>
                    </div>
                }
            </div>

            <h3>Semantic</h3>
            <div style="display: flex; gap: 4px; flex-wrap: wrap;">
                @for (color of semanticColors; track color.name) {
                    <div style="text-align: center;">
                        <div
                            [style.background]="color.value"
                            style="width: 80px; height: 48px; border-radius: 6px; border: 1px solid #e0e0e0;"
                        ></div>
                        <div style="font-size: 11px; margin-top: 4px; color: #666;">{{ color.name }}</div>
                    </div>
                }
            </div>

            <h3>Chart Colors</h3>
            <div style="display: flex; gap: 4px; flex-wrap: wrap;">
                @for (color of chartColors; track color.name) {
                    <div style="text-align: center;">
                        <div
                            [style.background]="color.value"
                            style="width: 80px; height: 48px; border-radius: 6px; border: 1px solid #e0e0e0;"
                        ></div>
                        <div style="font-size: 11px; margin-top: 4px; color: #666;">{{ color.name }}</div>
                    </div>
                }
            </div>

            <h2>Layout Tokens</h2>
            <table style="border-collapse: collapse; width: 100%;">
                <thead>
                    <tr>
                        <th style="text-align: left; padding: 8px; border-bottom: 2px solid #e0e0e0;">Token</th>
                        <th style="text-align: left; padding: 8px; border-bottom: 2px solid #e0e0e0;">Value</th>
                    </tr>
                </thead>
                <tbody>
                    @for (token of layoutTokens; track token.name) {
                        <tr>
                            <td style="padding: 8px; border-bottom: 1px solid #f0f0f0; font-family: monospace; font-size: 13px;">
                                {{ token.name }}
                            </td>
                            <td style="padding: 8px; border-bottom: 1px solid #f0f0f0;">{{ token.value }}</td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    `,
})
class DesignTokensDocsComponent {
    protected readonly primaryShades = this.extractShades(DESIGN_TOKEN_VALUES.color.primary);
    protected readonly secondaryShades = this.extractShades(DESIGN_TOKEN_VALUES.color.secondary);
    protected readonly grayShades = this.extractShades(DESIGN_TOKEN_VALUES.color.gray);

    protected readonly semanticColors = Object.entries(DESIGN_TOKEN_VALUES.color.semantic).map(([name, value]) => ({ name, value }));

    protected readonly chartColors = Object.entries(DESIGN_TOKEN_VALUES.color.chart).map(([name, value]) => ({ name, value }));

    protected readonly layoutTokens = [
        { name: 'page.background', value: DESIGN_TOKEN_VALUES.layout.page.background },
        { name: 'page.horizontalPadding', value: DESIGN_TOKEN_VALUES.layout.page.horizontalPadding },
        { name: 'page.verticalPadding', value: DESIGN_TOKEN_VALUES.layout.page.verticalPadding },
        { name: 'page.contentMaxWidth', value: DESIGN_TOKEN_VALUES.layout.page.contentMaxWidth },
        { name: 'page.sectionSpacing', value: DESIGN_TOKEN_VALUES.layout.page.sectionSpacing },
        { name: 'header.height', value: DESIGN_TOKEN_VALUES.layout.header.height },
        { name: 'header.background', value: DESIGN_TOKEN_VALUES.layout.header.background },
        { name: 'header.textColor', value: DESIGN_TOKEN_VALUES.layout.header.textColor },
    ];

    private extractShades(palette: Record<string, string>): Array<{ name: string; value: string }> {
        return Object.entries(palette).map(([name, value]) => ({ name, value }));
    }
}

const meta: Meta<DesignTokensDocsComponent> = {
    title: 'Foundation/Design Tokens',
    component: DesignTokensDocsComponent,
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<DesignTokensDocsComponent>;

export const Colors: Story = {};
