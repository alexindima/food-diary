import { Component, input } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

type PlaybookSection = {
    title: string;
    description: string | null;
    items: string[];
};

@Component({
    selector: 'fd-ux-playbook-docs',
    standalone: true,
    template: `
        <div
            style="
                height: 100vh;
                overflow: auto;
                box-sizing: border-box;
                padding: var(--fd-space-xl) var(--fd-space-xl) calc(var(--fd-space-xl) * 2);
                font-family: var(--fd-font-family-base);
                color: #1f2937;
                background: #f8fafc;
            "
        >
            <div style="max-width: 960px;">
                <h1 style="margin: 0 0 var(--fd-space-md); font-size: var(--fd-text-metric-lg-size);">{{ pageTitle() }}</h1>
                <p style="margin: 0 0 var(--fd-space-lg); color: #4b5563; font-size: var(--fd-text-body-sm-size); line-height: 1.6;">
                    {{ intro() }}
                </p>

                @for (section of sections(); track section.title) {
                    <section
                        style="
                            margin-bottom: var(--fd-space-lg);
                            padding: var(--fd-space-lg);
                            border: 1px solid #e5e7eb;
                            border-radius: var(--fd-radius-card);
                            background: #fff;
                        "
                    >
                        <h2 style="margin: 0 0 var(--fd-space-sm); font-size: var(--fd-text-section-title-size);">{{ section.title }}</h2>
                        @if (section.description) {
                            <p style="margin: 0 0 var(--fd-space-sm); color: #4b5563; line-height: 1.6;">{{ section.description }}</p>
                        }
                        <ul style="margin: 0; padding-left: var(--fd-space-lg); display: grid; gap: var(--fd-space-xs); line-height: 1.6;">
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
class UxPlaybookDocsComponent {
    public readonly pageTitle = input('Frontend UX Playbook');
    public readonly intro = input(
        'Default async-state and feedback rules for the Food Diary frontend. Use these rules when building new pages or normalizing existing flows.',
    );
    public readonly sections = input<PlaybookSection[]>([]);
}

const coreSections: PlaybookSection[] = [
    {
        title: 'Page Bootstrap',
        description: 'Use layout-aware placeholders when the shape of the page is already known.',
        items: [
            'Prefer skeletons over plain Loading text for lists, cards, charts, and settings pages.',
            'Use section-level skeletons when widgets hydrate independently.',
            'Reserve plain loaders for simple transitional areas or embedded widgets.',
        ],
    },
    {
        title: 'Long GET Requests',
        description: 'Global loading should explain background work without replacing local UI.',
        items: [
            'Use the global top loader for long-running read requests.',
            'Silent background sync requests should opt out of the global top loader.',
            'Do not rely on the top loader alone when a page section can show its own placeholder.',
        ],
    },
    {
        title: 'Mutations',
        description: 'User-triggered actions need local, explicit feedback.',
        items: [
            'Use button loading for submit, save, create, delete, resend, retry, and confirm actions.',
            'Keep button width stable while loading.',
            'Use top loader as secondary feedback for follow-up refetches, not as the only signal.',
        ],
    },
    {
        title: 'Autosave and Settings',
        description: 'Autosave should be visible, but lighter than explicit submit flows.',
        items: [
            'Do not use button loading for autosave-only flows.',
            'Use a lightweight inline status such as Saving, Saved, or Sync failed.',
            'Avoid mixing unrelated save patterns inside the same settings section unless there is a strong product reason.',
        ],
    },
    {
        title: 'Error and Empty States',
        description: 'Load failures and no-data states must be deliberate and distinct.',
        items: [
            'Use fd-error-state for page or section load failures with retry.',
            'Keep validation errors inline near the relevant controls.',
            'Separate empty state from no-results state. Empty means no data yet; no-results means filters returned nothing.',
        ],
    },
    {
        title: 'Success and Destructive Actions',
        description: 'Do not over-notify, but do not hide risky work either.',
        items: [
            'Use toast for completed actions that stay on the same screen and are not already obvious from the UI.',
            'Avoid redundant success signals if the updated UI already proves success.',
            'Use confirm dialogs before destructive or hard-to-reverse actions, and show button loading on the confirm action.',
        ],
    },
    {
        title: 'Default Screen Patterns',
        description: 'These are the expected baselines by page type.',
        items: [
            'List pages: header, filters, skeleton grid, error state, empty state, no-results state, pagination or load-more feedback.',
            'Form pages: inline field errors, button loading for primary action, error state for initial load failure.',
            'Dashboard pages: defer heavy sections and keep the page responsive while individual widgets load.',
            'Settings pages: choose one clear save pattern per section and expose failure states visibly.',
        ],
    },
    {
        title: 'Anti-Patterns',
        description: null,
        items: [
            'Plain Loading text as the only state on a structured page.',
            'Global top loader as the only feedback for a clicked button.',
            'Hidden autosave failures.',
            'Destructive actions without confirm or pending feedback.',
            'Using the same visual treatment for empty state and no-results state.',
        ],
    },
];

const pickSection = (title: string): PlaybookSection[] => coreSections.filter(section => section.title === title);

const meta: Meta<UxPlaybookDocsComponent> = {
    title: 'Foundation/UX Playbook',
    component: UxPlaybookDocsComponent,
    tags: ['autodocs'],
    args: {
        pageTitle: 'Frontend UX Playbook',
        intro: 'Default async-state and feedback rules for the Food Diary frontend. Use these rules when building new pages or normalizing existing flows.',
    },
    parameters: {
        layout: 'fullscreen',
        controls: {
            disable: true,
        },
        docs: {
            description: {
                component: 'Shared UX rules for async states, errors, empty states, autosave, and destructive actions.',
            },
        },
    },
};

export default meta;
type Story = StoryObj<UxPlaybookDocsComponent>;

export const Overview: Story = {
    args: {
        pageTitle: 'Frontend UX Playbook',
        intro: 'High-level overview of the shared UX rules. Open the sub-pages in this section when you need a focused rule set.',
        sections: [
            {
                title: 'How to Use This Playbook',
                description: 'Use the overview for orientation, then open the dedicated sub-pages for the specific rule set you need.',
                items: [
                    'Start with Page Bootstrap when designing a new page shell.',
                    'Open Mutations when wiring save, submit, delete, resend, or retry actions.',
                    'Open Error and Empty States when a page loads remote data.',
                    'Use Anti-Patterns as a final review checklist before merge.',
                ],
            },
            ...coreSections,
        ],
    },
};

export const PageBootstrap: Story = {
    name: 'Page Bootstrap',
    args: {
        pageTitle: 'Page Bootstrap',
        intro: 'Rules for first-load page states and section hydration.',
        sections: pickSection('Page Bootstrap'),
    },
};

export const LongGetRequests: Story = {
    name: 'Long GET Requests',
    args: {
        pageTitle: 'Long GET Requests',
        intro: 'Rules for global loading and long-running read requests.',
        sections: pickSection('Long GET Requests'),
    },
};

export const Mutations: Story = {
    args: {
        pageTitle: 'Mutations',
        intro: 'Rules for user-triggered actions such as save, submit, create, delete, resend, retry, and confirm.',
        sections: pickSection('Mutations'),
    },
};

export const AutosaveAndSettings: Story = {
    name: 'Autosave and Settings',
    args: {
        pageTitle: 'Autosave and Settings',
        intro: 'Rules for autosave feedback and settings sections with mixed controls.',
        sections: pickSection('Autosave and Settings'),
    },
};

export const ErrorAndEmptyStates: Story = {
    name: 'Error and Empty States',
    args: {
        pageTitle: 'Error and Empty States',
        intro: 'Rules for retry, page failures, empty data, and no-results states.',
        sections: pickSection('Error and Empty States'),
    },
};

export const SuccessAndDestructiveActions: Story = {
    name: 'Success and Destructive Actions',
    args: {
        pageTitle: 'Success and Destructive Actions',
        intro: 'Rules for toast usage, confirm dialogs, and destructive flows.',
        sections: pickSection('Success and Destructive Actions'),
    },
};

export const DefaultScreenPatterns: Story = {
    name: 'Default Screen Patterns',
    args: {
        pageTitle: 'Default Screen Patterns',
        intro: 'Baseline expectations for list pages, form pages, dashboard pages, and settings pages.',
        sections: pickSection('Default Screen Patterns'),
    },
};

export const AntiPatterns: Story = {
    name: 'Anti-Patterns',
    args: {
        pageTitle: 'Anti-Patterns',
        intro: 'Quick review checklist for patterns that should not appear in new UI work.',
        sections: pickSection('Anti-Patterns'),
    },
};
