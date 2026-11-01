import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, input } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular-vite';

import { FdUiCardComponent } from './card/fd-ui-card';

@Component({
    selector: 'fd-data-table-preview',
    imports: [FdUiCardComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <div style="padding: var(--fd-space-page-card-gap); background: var(--fd-bg-page); color: var(--fd-color-text);">
            <fd-ui-card title="Compact summary table" style="display: block; max-width: 36rem;">
                <table class="fd-data-table" aria-label="Nutrition overview">
                    <thead>
                        <tr>
                            <th scope="col">Metric</th>
                            <th class="fd-data-table__number" scope="col">Average</th>
                            <th class="fd-data-table__number" scope="col">Base goal</th>
                        </tr>
                    </thead>
                    <tbody>
                        @for (row of rows; track row.label) {
                            <tr>
                                <th scope="row">{{ row.label }}</th>
                                <td class="fd-data-table__number">{{ row.average }}</td>
                                <td class="fd-data-table__number fd-ui-text-muted">{{ row.goal }}</td>
                            </tr>
                        }
                    </tbody>
                </table>
            </fd-ui-card>
        </div>
    `,
})
class DataTablePreviewComponent {
    public readonly theme = input<'ocean' | 'leaf' | 'dark'>('ocean');
    public readonly uiStyle = input<'classic' | 'modern'>('classic');
    protected readonly rows = [
        { label: 'Calories', average: '485 kcal', goal: '2,500 kcal' },
        { label: 'Protein', average: '10.4 g', goal: '150 g' },
        { label: 'Fat', average: '2.8 g', goal: '65 g' },
        { label: 'Carbohydrates', average: '111.5 g', goal: '200 g' },
        { label: 'Fiber', average: '14 g', goal: 'Not set' },
    ];

    public constructor() {
        const root = inject(DOCUMENT).documentElement;
        const previousTheme = root.getAttribute('data-theme');
        const previousStyle = root.getAttribute('data-ui-style');
        effect(() => {
            root.setAttribute('data-theme', this.theme());
            root.setAttribute('data-ui-style', this.uiStyle());
        });
        inject(DestroyRef).onDestroy(() => {
            if (previousTheme === null) {
                root.removeAttribute('data-theme');
            } else {
                root.setAttribute('data-theme', previousTheme);
            }
            if (previousStyle === null) {
                root.removeAttribute('data-ui-style');
            } else {
                root.setAttribute('data-ui-style', previousStyle);
            }
        });
    }
}

const meta: Meta<DataTablePreviewComponent> = {
    title: 'Foundation/Data tables',
    component: DataTablePreviewComponent,
    tags: ['autodocs'],
    argTypes: {
        theme: { control: 'select', options: ['ocean', 'leaf', 'dark'] },
        uiStyle: { control: 'select', options: ['classic', 'modern'] },
    },
};

export default meta;
type Story = StoryObj<DataTablePreviewComponent>;

export const Compact: Story = { args: { theme: 'ocean', uiStyle: 'classic' } };
export const Dark: Story = { args: { theme: 'dark', uiStyle: 'classic' } };
export const Modern: Story = { args: { theme: 'ocean', uiStyle: 'modern' } };
