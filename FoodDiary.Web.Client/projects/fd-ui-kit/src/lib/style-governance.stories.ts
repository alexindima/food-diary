import { Component, input } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

type GovernanceSection = {
    title: string;
    summary: string;
    doItems: string[];
    avoidItems: string[];
};

@Component({
    selector: 'fd-style-governance-docs',
    standalone: true,
    template: `
        <div
            style="
                min-height: 100vh;
                overflow: auto;
                box-sizing: border-box;
                padding: var(--fd-space-xl) var(--fd-space-xl) calc(var(--fd-space-xl) * 2);
                font-family: var(--fd-font-family-base);
                color: var(--fd-color-text);
                background: var(--fd-bg-page);
            "
        >
            <div style="max-width: var(--fd-layout-page-content-max-width);">
                <header style="margin-bottom: var(--fd-space-xl);">
                    <h1 style="margin: 0 0 var(--fd-space-sm); font-size: var(--fd-text-metric-lg-size);">
                        {{ pageTitle() }}
                    </h1>
                    <p style="margin: 0; max-width: 76ch; color: var(--fd-color-text-muted); line-height: var(--fd-text-body-line-height);">
                        {{ intro() }}
                    </p>
                </header>

                <section
                    style="
                        margin-bottom: var(--fd-space-lg);
                        padding: var(--fd-space-lg);
                        border: var(--fd-border-muted);
                        border-radius: var(--fd-radius-card);
                        background: var(--fd-bg-surface-raised);
                        box-shadow: var(--fd-shadow-sm);
                    "
                >
                    <h2 style="margin: 0 0 var(--fd-space-sm); font-size: var(--fd-text-section-title-size);">Decision Order</h2>
                    <ol style="margin: 0; padding-left: var(--fd-space-lg); display: grid; gap: var(--fd-space-xs); line-height: 1.6;">
                        @for (item of decisionOrder(); track item) {
                            <li>{{ item }}</li>
                        }
                    </ol>
                </section>

                <div style="display: grid; gap: var(--fd-space-lg);">
                    @for (section of sections(); track section.title) {
                        <section
                            style="
                                padding: var(--fd-space-lg);
                                border: var(--fd-border-muted);
                                border-radius: var(--fd-radius-card);
                                background: var(--fd-bg-surface-raised);
                                box-shadow: var(--fd-shadow-sm);
                            "
                        >
                            <h2 style="margin: 0 0 var(--fd-space-xs); font-size: var(--fd-text-section-title-size);">
                                {{ section.title }}
                            </h2>
                            <p style="margin: 0 0 var(--fd-space-md); color: var(--fd-color-text-muted); line-height: 1.6;">
                                {{ section.summary }}
                            </p>

                            <div
                                style="display: grid; gap: var(--fd-space-md); grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));"
                            >
                                <div>
                                    <h3 style="margin: 0 0 var(--fd-space-xs); font-size: var(--fd-text-card-title-size);">Do</h3>
                                    <ul
                                        style="margin: 0; padding-left: var(--fd-space-lg); display: grid; gap: var(--fd-space-xs); line-height: 1.6;"
                                    >
                                        @for (item of section.doItems; track item) {
                                            <li>{{ item }}</li>
                                        }
                                    </ul>
                                </div>
                                <div>
                                    <h3 style="margin: 0 0 var(--fd-space-xs); font-size: var(--fd-text-card-title-size);">Avoid</h3>
                                    <ul
                                        style="margin: 0; padding-left: var(--fd-space-lg); display: grid; gap: var(--fd-space-xs); line-height: 1.6;"
                                    >
                                        @for (item of section.avoidItems; track item) {
                                            <li>{{ item }}</li>
                                        }
                                    </ul>
                                </div>
                            </div>
                        </section>
                    }
                </div>
            </div>
        </div>
    `,
})
class StyleGovernanceDocsComponent {
    public readonly pageTitle = input('Style Governance');
    public readonly intro = input(
        'Rules for keeping frontend styles centralized. Use this page as a review checklist before adding component SCSS or new design tokens.',
    );
    public readonly decisionOrder = input<string[]>([]);
    public readonly sections = input<GovernanceSection[]>([]);
}

const decisionOrder = [
    'Use an existing fd-ui-kit component when the behavior or visual primitive already exists.',
    'Use global utility classes for repeated layout, typography, surface, effect, and state patterns.',
    'Use CSS design tokens for spacing, sizing, typography, radii, colors, backgrounds, borders, shadows, and effects.',
    'Write component SCSS only for local structure, responsive layout, and one-off geometry that should stay local.',
];

const sections: GovernanceSection[] = [
    {
        title: 'Design Tokens',
        summary:
            'Tokens are the source of truth for values that should change consistently across themes, density, typography, and UI modes.',
        doItems: [
            'Use var(--fd-space-*), var(--fd-size-*), var(--fd-radius-*), var(--fd-text-*), var(--fd-color-*), var(--fd-bg-*), var(--fd-border-*), and var(--fd-shadow-*).',
            'Add a token when a value repeats across components or belongs to a reusable primitive.',
            'Fix missing tokens in src/styles/design-tokens.scss instead of adding local fallbacks.',
        ],
        avoidItems: [
            'Do not use var(--fd-..., fallback) for required design tokens.',
            'Do not hardcode repeated non-zero spacing, control sizes, radii, shadows, or colors.',
            'Do not move one-off chart, SVG, canvas, hero, or container geometry into global tokens.',
        ],
    },
    {
        title: 'Utility Classes',
        summary: 'Utilities are for common composition patterns that should not be copied into component styles.',
        doItems: [
            'Use utilities for repeated typography, stack spacing, surfaces, borders, shadows, and state styles.',
            'Create a utility when multiple unrelated components need the same styling intent.',
            'Keep utilities token-driven and narrow in purpose.',
        ],
        avoidItems: [
            'Do not create utility classes for a layout that only exists in one component.',
            'Do not use utilities to hide missing UI kit component APIs.',
            'Do not duplicate a utility rule inside component SCSS.',
        ],
    },
    {
        title: 'Component SCSS',
        summary: 'Component styles should describe local structure, not recreate the design system.',
        doItems: [
            'Keep host display, local grid/flex structure, and responsive behavior in the component.',
            'Use @use variables only for Sass media aliases such as @media #{variables.$media-tablet}.',
            'Promote duplicated behavior into fd-ui-kit or a utility class.',
        ],
        avoidItems: [
            'Do not use variables.scss as a runtime style source.',
            'Do not add raw hex/rgb colors outside token definitions.',
            'Do not add page-level one-off overrides when the behavior belongs in fd-ui-kit.',
        ],
    },
    {
        title: 'Review Checklist',
        summary: 'Run this checklist when reviewing frontend style changes.',
        doItems: [
            'Scan for raw colors, token fallbacks, hardcoded spacing, and repeated hardcoded control sizes.',
            'Check whether a new component style should be a token, utility class, or UI kit API instead.',
            'Update Storybook docs when adding shared token groups, utility patterns, or visual primitives.',
        ],
        avoidItems: [
            'Do not approve new style islands that duplicate existing tokenized patterns.',
            'Do not centralize browser reset values such as 0, auto, 100%, 1fr, or 1em.',
            'Do not require tokens for values that are local visualization math.',
        ],
    },
];

const meta: Meta<StyleGovernanceDocsComponent> = {
    title: 'Foundation/Style Governance',
    component: StyleGovernanceDocsComponent,
    tags: ['autodocs'],
    args: {
        pageTitle: 'Style Governance',
        intro: 'Rules for keeping frontend styles centralized. Use this page as a review checklist before adding component SCSS or new design tokens.',
        decisionOrder,
        sections,
    },
    parameters: {
        layout: 'fullscreen',
        controls: {
            disable: true,
        },
        docs: {
            description: {
                component: 'Design-system guardrails for tokens, utility classes, component SCSS, and style reviews.',
            },
        },
    },
};

export default meta;
type Story = StoryObj<StyleGovernanceDocsComponent>;

export const Overview: Story = {};

export const Tokens: Story = {
    args: {
        pageTitle: 'Design Token Rules',
        intro: 'Use tokens for shared values and theme-aware styling. Do not add token fallbacks for required design-system values.',
        decisionOrder,
        sections: sections.filter(section => section.title === 'Design Tokens'),
    },
};

export const Utilities: Story = {
    args: {
        pageTitle: 'Utility Class Rules',
        intro: 'Use utilities for repeated composition patterns that should not live in component SCSS.',
        decisionOrder,
        sections: sections.filter(section => section.title === 'Utility Classes'),
    },
};

export const ComponentScss: Story = {
    name: 'Component SCSS',
    args: {
        pageTitle: 'Component SCSS Rules',
        intro: 'Component styles should stay local and structural. Shared visual behavior belongs in tokens, utilities, or fd-ui-kit.',
        decisionOrder,
        sections: sections.filter(section => section.title === 'Component SCSS'),
    },
};

export const ReviewChecklist: Story = {
    name: 'Review Checklist',
    args: {
        pageTitle: 'Style Review Checklist',
        intro: 'Use this checklist before merging frontend style changes.',
        decisionOrder,
        sections: sections.filter(section => section.title === 'Review Checklist'),
    },
};
