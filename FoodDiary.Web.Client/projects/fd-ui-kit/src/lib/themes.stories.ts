import { DOCUMENT } from '@angular/common';
import { Component, DestroyRef, effect, inject, input } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

type ThemeName = 'ocean' | 'leaf' | 'dark';
type UiStyleName = 'classic' | 'modern';

type ThemeHighlight = {
    label: string;
    token: string;
};

@Component({
    selector: 'fd-theme-docs',
    standalone: true,
    template: `
        <div
            style="
                height: 100vh;
                overflow: auto;
                box-sizing: border-box;
                padding: 32px 32px 64px;
                font-family: var(--fd-font-family-base, Inter, system-ui, sans-serif);
                color: var(--fd-color-text, #1f2937);
                background: var(--fd-layout-page-background, #f8fafc);
            "
        >
            <div style="max-width: 1080px; margin: 0 auto;">
                <h1 style="margin: 0 0 16px; font-size: 28px;">{{ pageTitle() }}</h1>
                <p
                    style="
                        margin: 0 0 24px;
                        color: var(--fd-color-text-muted, #4b5563);
                        font-size: 15px;
                        line-height: 1.6;
                    "
                >
                    {{ intro() }}
                </p>

                @if (notes().length > 0) {
                    <section
                        style="
                            margin-bottom: 24px;
                            padding: 20px;
                            border: 1px solid var(--fd-color-border-strong, #dbe3f0);
                            border-radius: 18px;
                            background: var(--fd-color-surface-raised, #ffffff);
                            box-shadow: 0 14px 32px color-mix(in srgb, var(--fd-color-primary-700, #334155) 8%, transparent);
                        "
                    >
                        <h2 style="margin: 0 0 12px; font-size: 20px;">How This Theme Feels</h2>
                        <ul style="margin: 0; padding-left: 20px; display: grid; gap: 8px; line-height: 1.6;">
                            @for (note of notes(); track note) {
                                <li>{{ note }}</li>
                            }
                        </ul>
                    </section>
                }

                @if (highlights().length > 0) {
                    <section
                        style="
                            margin-bottom: 24px;
                            padding: 20px;
                            border: 1px solid var(--fd-color-border-strong, #dbe3f0);
                            border-radius: 18px;
                            background: var(--fd-color-surface-raised, #ffffff);
                        "
                    >
                        <h2 style="margin: 0 0 12px; font-size: 20px;">Theme Highlights</h2>
                        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px;">
                            @for (highlight of highlights(); track highlight.label) {
                                <div
                                    style="
                                        padding: 14px;
                                        border-radius: 14px;
                                        border: 1px solid color-mix(in srgb, var(--fd-color-border-strong, #dbe3f0) 72%, transparent);
                                        background: color-mix(in srgb, var(--fd-color-surface, #fff) 92%, transparent);
                                    "
                                >
                                    <div style="display: flex; align-items: center; gap: 10px;">
                                        <div
                                            [style.background]="highlight.token"
                                            style="
                                                width: 28px;
                                                height: 28px;
                                                border-radius: 999px;
                                                border: 1px solid color-mix(in srgb, var(--fd-color-border-strong, #dbe3f0) 72%, transparent);
                                                flex: none;
                                            "
                                        ></div>
                                        <div>
                                            <div style="font-weight: 600;">{{ highlight.label }}</div>
                                            <div
                                                style="
                                                    font-size: 12px;
                                                    color: var(--fd-color-text-muted, #4b5563);
                                                    font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
                                                "
                                            >
                                                {{ highlight.token }}
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            }
                        </div>
                    </section>
                }

                <section
                    style="
                        margin-bottom: 24px;
                        padding: 24px;
                        border: 1px solid var(--fd-color-border-strong, #dbe3f0);
                        border-radius: 24px;
                        background:
                            radial-gradient(circle at top right, color-mix(in srgb, var(--fd-color-primary-500, #6366f1) 14%, transparent), transparent 36%),
                            linear-gradient(180deg, color-mix(in srgb, var(--fd-color-primary-50, #eef2ff) 55%, var(--fd-layout-page-background, #f8fafc)) 0%, var(--fd-layout-page-background, #f8fafc) 100%);
                        box-shadow: 0 20px 44px color-mix(in srgb, var(--fd-color-primary-700, #334155) 10%, transparent);
                    "
                >
                    <div
                        style="
                            display: flex;
                            justify-content: space-between;
                            align-items: flex-start;
                            gap: 16px;
                            margin-bottom: 18px;
                            flex-wrap: wrap;
                        "
                    >
                        <div>
                            <div
                                style="
                                    display: inline-flex;
                                    align-items: center;
                                    gap: 8px;
                                    padding: 6px 10px;
                                    border-radius: 999px;
                                    background: color-mix(in srgb, var(--fd-color-primary-500, #6366f1) 12%, transparent);
                                    color: var(--fd-color-primary-700, #4338ca);
                                    font-size: 13px;
                                    font-weight: 600;
                                    margin-bottom: 12px;
                                "
                            >
                                {{ badgeLabel() }}
                            </div>
                            <h2 style="margin: 0 0 8px; font-size: 24px;">{{ previewTitle() }}</h2>
                            <p
                                style="
                                    margin: 0;
                                    max-width: 640px;
                                    color: var(--fd-color-text-muted, #4b5563);
                                    line-height: 1.6;
                                "
                            >
                                {{ previewDescription() }}
                            </p>
                        </div>
                        <div
                            style="
                                min-width: 180px;
                                padding: 14px 16px;
                                border-radius: 18px;
                                border: 1px solid color-mix(in srgb, var(--fd-color-primary-500, #6366f1) 18%, transparent);
                                background: color-mix(in srgb, var(--fd-color-surface-raised, #fff) 92%, var(--fd-color-primary-50, #eef2ff));
                            "
                        >
                            <div style="font-size: 12px; color: var(--fd-color-text-muted, #4b5563); margin-bottom: 6px;">Active Setup</div>
                            <div style="font-weight: 700;">Theme: {{ theme() }}</div>
                            <div style="font-weight: 700;">UI Style: {{ uiStyle() }}</div>
                        </div>
                    </div>

                    <div
                        style="
                            display: grid;
                            grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
                            gap: 16px;
                        "
                    >
                        <div
                            style="
                                padding: 18px;
                                border-radius: 20px;
                                border: 1px solid color-mix(in srgb, var(--fd-color-border-strong, #dbe3f0) 78%, transparent);
                                background: var(--fd-color-surface-raised, #ffffff);
                            "
                        >
                            <div
                                style="font-size: 12px; text-transform: uppercase; letter-spacing: 0.08em; color: var(--fd-color-text-muted, #4b5563); margin-bottom: 12px;"
                            >
                                Page Surface
                            </div>
                            <div style="display: flex; gap: 10px; margin-bottom: 14px;">
                                <div
                                    style="flex: 1; height: 72px; border-radius: 16px; background: var(--fd-color-surface-sunken, #eef2f7);"
                                ></div>
                                <div
                                    style="
                                        width: 72px;
                                        height: 72px;
                                        border-radius: 16px;
                                        background: linear-gradient(135deg, var(--fd-color-primary-500, #6366f1), var(--fd-color-primary-300, #a5b4fc));
                                    "
                                ></div>
                            </div>
                            <div style="font-weight: 600; margin-bottom: 6px;">Dense information, clear hierarchy</div>
                            <div style="color: var(--fd-color-text-muted, #4b5563); line-height: 1.6;">
                                Good for app shells, analytics surfaces, settings pages, and daily-use flows.
                            </div>
                        </div>

                        <div
                            style="
                                padding: 18px;
                                border-radius: 20px;
                                border: 1px solid color-mix(in srgb, var(--fd-color-border-strong, #dbe3f0) 78%, transparent);
                                background: var(--fd-color-surface-raised, #ffffff);
                            "
                        >
                            <div
                                style="font-size: 12px; text-transform: uppercase; letter-spacing: 0.08em; color: var(--fd-color-text-muted, #4b5563); margin-bottom: 12px;"
                            >
                                Component Preview
                            </div>
                            <div style="display: grid; gap: 12px;">
                                <button
                                    type="button"
                                    style="
                                        appearance: none;
                                        border: none;
                                        border-radius: var(--fd-button-radius-md, 14px);
                                        background: var(--fd-button-primary-background-default, var(--fd-color-primary-600, #4f46e5));
                                        color: var(--fd-button-primary-text-default, #ffffff);
                                        padding: 12px 16px;
                                        font: inherit;
                                        font-weight: 600;
                                        text-align: left;
                                        box-shadow: 0 14px 24px color-mix(in srgb, var(--fd-color-primary-700, #4338ca) 18%, transparent);
                                    "
                                >
                                    Primary action
                                </button>

                                <label style="display: grid; gap: 6px;">
                                    <span style="font-size: 13px; font-weight: 600;">Input field</span>
                                    <input
                                        type="text"
                                        value="Preview value"
                                        readonly
                                        style="
                                            width: 100%;
                                            border-radius: var(--fd-input-radius-md, 14px);
                                            border: 1px solid var(--fd-color-border-strong, #dbe3f0);
                                            background: var(--fd-color-surface, #ffffff);
                                            color: var(--fd-color-text, #1f2937);
                                            padding: 12px 14px;
                                            font: inherit;
                                        "
                                    />
                                </label>

                                <div
                                    style="
                                        padding: 14px;
                                        border-radius: 16px;
                                        background: color-mix(in srgb, var(--fd-color-success, #22c55e) 12%, var(--fd-color-surface, #fff));
                                        border: 1px solid color-mix(in srgb, var(--fd-color-success, #22c55e) 28%, transparent);
                                        color: var(--fd-color-text, #1f2937);
                                    "
                                >
                                    Success / status surface
                                </div>
                            </div>
                        </div>
                    </div>
                </section>
            </div>
        </div>
    `,
})
class ThemeDocsComponent {
    private readonly document = inject(DOCUMENT);
    private readonly destroyRef = inject(DestroyRef);

    public readonly pageTitle = input('Themes');
    public readonly intro = input('Visual themes control the product mood. UI styles control component density and surface treatment.');
    public readonly badgeLabel = input('Theme preview');
    public readonly previewTitle = input('Theme sample');
    public readonly previewDescription = input('A quick preview of page shell, primary action, field chrome, and state surfaces.');
    public readonly theme = input<ThemeName>('ocean');
    public readonly uiStyle = input<UiStyleName>('classic');
    public readonly notes = input<string[]>([]);
    public readonly highlights = input<ThemeHighlight[]>([]);

    public constructor() {
        const root = this.document.documentElement;

        effect(() => {
            root.setAttribute('data-theme', this.theme());
            root.setAttribute('data-ui-style', this.uiStyle());
        });

        this.destroyRef.onDestroy(() => {
            root.setAttribute('data-theme', 'ocean');
            root.setAttribute('data-ui-style', 'classic');
        });
    }
}

const overviewHighlights: ThemeHighlight[] = [
    { label: 'Ocean primary', token: 'var(--fd-color-primary-500)' },
    { label: 'Raised surface', token: 'var(--fd-color-surface-raised)' },
    { label: 'Border', token: 'var(--fd-color-border-strong)' },
];

const oceanHighlights: ThemeHighlight[] = [
    { label: 'Primary', token: 'var(--fd-color-primary-500)' },
    { label: 'Secondary', token: 'var(--fd-color-secondary-500)' },
    { label: 'Info', token: 'var(--fd-color-info)' },
];

const leafHighlights: ThemeHighlight[] = [
    { label: 'Primary', token: 'var(--fd-color-primary-500)' },
    { label: 'Secondary', token: 'var(--fd-color-secondary-500)' },
    { label: 'Success', token: 'var(--fd-color-success)' },
];

const darkHighlights: ThemeHighlight[] = [
    { label: 'Primary', token: 'var(--fd-color-primary-500)' },
    { label: 'Surface', token: 'var(--fd-color-surface-raised)' },
    { label: 'Text', token: 'var(--fd-color-text)' },
];

const meta: Meta<ThemeDocsComponent> = {
    title: 'Foundation/Themes',
    component: ThemeDocsComponent,
    tags: ['autodocs'],
    parameters: {
        layout: 'fullscreen',
        controls: {
            disable: true,
        },
    },
};

export default meta;
type Story = StoryObj<ThemeDocsComponent>;

export const Overview: Story = {
    args: {
        pageTitle: 'Themes Overview',
        intro: 'Food Diary currently ships with three application themes and two UI styles. Use the sub-pages in this section to inspect each theme in isolation.',
        theme: 'ocean',
        uiStyle: 'classic',
        badgeLabel: 'Foundation',
        previewTitle: 'Shared preview surface',
        previewDescription:
            'The preview below uses real design tokens from the active theme. Theme changes affect color mood and contrast; UI style changes affect radii and component feel.',
        notes: [
            'Ocean is the default general-purpose product theme.',
            'Leaf shifts the product toward wellness and softer green accents.',
            'Dark is the high-contrast nighttime theme with darker surfaces and lighter text.',
            'Classic and modern should preserve the same information architecture while changing the UI treatment.',
        ],
        highlights: overviewHighlights,
    },
};

export const Ocean: Story = {
    args: {
        pageTitle: 'Ocean Theme',
        intro: 'Default product theme with cool blue-violet accents and balanced contrast for daily-use flows.',
        theme: 'ocean',
        uiStyle: 'classic',
        badgeLabel: 'Light theme',
        previewTitle: 'Ocean',
        previewDescription: 'Best default choice for neutral product surfaces, dashboards, and app-wide consistency.',
        notes: [
            'Feels calm, structured, and product-neutral.',
            'Works well for data-heavy screens and broad default usage.',
            'Good baseline when you do not want the visual tone to overtake the content.',
        ],
        highlights: oceanHighlights,
    },
};

export const Leaf: Story = {
    args: {
        pageTitle: 'Leaf Theme',
        intro: 'Health-oriented theme with greener accents and a softer, more wellness-driven mood.',
        theme: 'leaf',
        uiStyle: 'classic',
        badgeLabel: 'Light theme',
        previewTitle: 'Leaf',
        previewDescription: 'Useful when the product should feel more natural, supportive, and habit-oriented.',
        notes: [
            'Feels warmer and more organic than Ocean.',
            'Good fit for wellness, fasting, nutrition, and coaching-oriented flows.',
            'Use when a greener accent system supports the content rather than distracting from it.',
        ],
        highlights: leafHighlights,
    },
};

export const Dark: Story = {
    args: {
        pageTitle: 'Dark Theme',
        intro: 'Dark mode theme with reduced surface brightness and higher emphasis on luminous accents.',
        theme: 'dark',
        uiStyle: 'classic',
        badgeLabel: 'Dark theme',
        previewTitle: 'Dark',
        previewDescription: 'Built for low-light usage while preserving readable contrast and visible action emphasis.',
        notes: [
            'Feels focused, contrast-driven, and more dramatic.',
            'Important to audit semantic colors and focus states carefully in this theme.',
            'Best when users spend longer sessions in the app or prefer darker surfaces.',
        ],
        highlights: darkHighlights,
    },
};

export const UiStyles: Story = {
    name: 'UI Styles',
    args: {
        pageTitle: 'UI Styles',
        intro: 'UI styles layer on top of themes. They do not redefine the product mood; they shift component treatment, density, and radii.',
        theme: 'ocean',
        uiStyle: 'modern',
        badgeLabel: 'Style preview',
        previewTitle: 'Modern style on Ocean',
        previewDescription: 'Switch this story between classic and modern in code when documenting style-specific updates.',
        notes: [
            'Classic should feel closer to the original product shell and spacing language.',
            'Modern should feel more current through surface treatment, radii, and component chrome.',
            'Keep behavior, accessibility, and information hierarchy stable across both styles.',
        ],
        highlights: [
            { label: 'Primary', token: 'var(--fd-color-primary-500)' },
            { label: 'Surface', token: 'var(--fd-color-surface-raised)' },
            { label: 'Muted text', token: 'var(--fd-color-text-muted)' },
        ],
    },
};
