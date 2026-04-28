import { Component, input } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

type DialogPlaybookSection = {
    title: string;
    description: string | null;
    items: string[];
};

@Component({
    selector: 'fd-dialog-playbook-docs',
    standalone: true,
    template: `
        <div
            style="
                min-height: 100vh;
                box-sizing: border-box;
                padding: var(--fd-space-xl) var(--fd-space-xl) calc(var(--fd-space-xl) * 2);
                font-family: var(--fd-font-family-base);
                color: #1f2937;
                background:
                    radial-gradient(circle at top left, rgba(16, 185, 129, 0.12), transparent 32%),
                    radial-gradient(circle at top right, rgba(59, 130, 246, 0.1), transparent 26%),
                    #f8fafc;
            "
        >
            <div style="max-width: 1040px;">
                <h1 style="margin: 0 0 var(--fd-space-sm); font-size: var(--fd-text-metric-lg-size);">{{ pageTitle() }}</h1>
                <p style="margin: 0 0 var(--fd-space-lg); color: #4b5563; font-size: var(--fd-text-body-sm-size); line-height: 1.7;">
                    {{ intro() }}
                </p>

                <section
                    style="
                        margin-bottom: var(--fd-space-lg);
                        padding: var(--fd-space-md) var(--fd-space-lg);
                        border: 1px solid #dbeafe;
                        border-radius: var(--fd-radius-panel);
                        background: linear-gradient(135deg, rgba(255, 255, 255, 0.96), rgba(239, 246, 255, 0.92));
                    "
                >
                    <h2 style="margin: 0 0 var(--fd-space-xs); font-size: var(--fd-text-section-title-size);">Default Rule</h2>
                    <p style="margin: 0; color: #334155; line-height: 1.7;">
                        Prefer semantic <code>preset</code> for normal product flows. Leave the dialog manual only when it needs custom
                        width, custom panel/backdrop classes, media-heavy layout, or a special interaction model.
                    </p>
                </section>

                @for (section of sections(); track section.title) {
                    <section
                        style="
                            margin-bottom: var(--fd-space-lg);
                            padding: var(--fd-space-md) var(--fd-space-lg);
                            border: 1px solid #e5e7eb;
                            border-radius: var(--fd-radius-panel);
                            background: rgba(255, 255, 255, 0.96);
                            box-shadow: 0 10px 30px rgba(15, 23, 42, 0.05);
                        "
                    >
                        <h2 style="margin: 0 0 var(--fd-space-xs); font-size: var(--fd-text-section-title-size);">{{ section.title }}</h2>
                        @if (section.description) {
                            <p style="margin: 0 0 var(--fd-space-sm); color: #4b5563; line-height: 1.7;">{{ section.description }}</p>
                        }
                        <ul style="margin: 0; padding-left: var(--fd-space-lg); display: grid; gap: var(--fd-space-xs); line-height: 1.65;">
                            @for (item of section.items; track item) {
                                <li>{{ item }}</li>
                            }
                        </ul>
                    </section>
                }
            </div>
        </div>
    `,
})
class DialogPlaybookDocsComponent {
    public readonly pageTitle = input('Dialog Playbook');
    public readonly intro = input('Shared rules for choosing dialog presets, sizes, and escape hatches across the Food Diary frontend.');
    public readonly sections = input<DialogPlaybookSection[]>([]);
}

const dialogSections: DialogPlaybookSection[] = [
    {
        title: 'Preset First',
        description: 'Use a semantic preset unless the dialog is intentionally unusual.',
        items: [
            'Use confirm for short confirmations, destructive actions, premium gates, and simple success/decision dialogs.',
            'Use form for auth, settings, filters, and standard data-entry flows.',
            'Use list for selection lists, feeds, notification trays, and browse-first flows.',
            'Use detail for content-heavy detail overlays that need richer presentation than a plain form.',
            'Use fullscreen for editors, create flows, scanners, or selectors that feel cramped in a centered modal.',
        ],
    },
    {
        title: 'Size Scale',
        description: 'These sizes are the shared fallback scale when you are not using a preset or need to reason about width.',
        items: [
            'sm = 400px for short confirm content only.',
            'md = 640px for normal forms and settings.',
            'lg = 840px for richer forms, detail views, and list-heavy layouts.',
            'xl = 1120px for dense review flows, table-like content, and multi-column dialogs.',
            'Anything more demanding should usually be fullscreen instead of inventing another centered width.',
        ],
    },
    {
        title: 'When To Stay Manual',
        description: 'Not every modal should be forced into a preset.',
        items: [
            'Stay manual when the dialog needs explicit width or maxWidth tuning.',
            'Stay manual when it depends on custom panelClass or backdropClass.',
            'Stay manual for chart, media preview, barcode scanner, or AI/photo flows with unusual layout needs.',
            'Stay manual when the interaction model is not a standard confirm/form/list/detail/fullscreen shape.',
        ],
    },
    {
        title: 'Default Fallback',
        description: 'Specialized dialogs still need a predictable baseline.',
        items: [
            'If preset is omitted, FdUiDialogService uses the explicit size from config.',
            'If both preset and size are omitted, the dialog falls back to md.',
            'This means bespoke dialogs can stay manual without losing consistency.',
        ],
    },
    {
        title: 'Anti-Patterns',
        description: null,
        items: [
            'Using sm for a dialog that contains full forms, chips, textarea, or dense validation copy.',
            'Picking widths ad hoc in feature code when an existing preset already matches the flow.',
            'Creating one-off panel classes just to make a normal form wider instead of using md, lg, or xl.',
            'Keeping a cramped centered dialog when the flow is really an editor or selector and should be fullscreen.',
        ],
    },
];

const meta: Meta<DialogPlaybookDocsComponent> = {
    title: 'Foundation/Dialogs',
    component: DialogPlaybookDocsComponent,
    tags: ['autodocs'],
    args: {
        pageTitle: 'Dialog Playbook',
        intro: 'Shared rules for choosing dialog presets, sizes, and escape hatches across the Food Diary frontend.',
        sections: dialogSections,
    },
    parameters: {
        layout: 'fullscreen',
        controls: {
            disable: true,
        },
        docs: {
            description: {
                component: 'Dialog sizing and preset guidance for consistent modal behavior across the app.',
            },
        },
    },
};

export default meta;
type Story = StoryObj<DialogPlaybookDocsComponent>;

export const Overview: Story = {};
