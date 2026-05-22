import { provideRouter } from '@angular/router';
import type { Meta, StoryObj } from '@storybook/angular';
import { applicationConfig } from '@storybook/angular';

import { FdUiSidebarComponent } from './fd-ui-sidebar.component';

const meta: Meta<FdUiSidebarComponent> = {
    title: 'Components/Sidebar',
    component: FdUiSidebarComponent,
    tags: ['autodocs'],
    decorators: [
        applicationConfig({
            providers: [provideRouter([])],
        }),
    ],
    args: {
        brandTitle: 'FoodDiary',
        brandSubtitle: 'Admin',
        logoText: 'FD',
        pendingRoute: null,
        notificationBadge: 0,
        sections: [
            {
                id: 'primary',
                items: [
                    { id: 'dashboard', icon: 'dashboard', label: 'Dashboard', route: '/', exact: true },
                    { id: 'users', icon: 'group', label: 'Users', route: '/users' },
                    { id: 'billing', icon: 'payments', label: 'Billing', route: '/billing' },
                ],
            },
            {
                id: 'operations',
                title: 'Operations',
                expanded: true,
                collapsible: true,
                secondary: true,
                items: [
                    { id: 'mail-inbox', icon: 'inbox', label: 'Mail inbox', route: '/mail-inbox' },
                    { id: 'moderation', icon: 'gavel', label: 'Moderation', route: '/moderation' },
                ],
            },
        ],
    },
    render: args => ({
        props: args,
        template: `
            <div style="display: grid; grid-template-columns: 280px minmax(0, 1fr); min-height: 720px; background: var(--fd-bg-page);">
                <fd-ui-sidebar
                    [brandTitle]="brandTitle"
                    [brandSubtitle]="brandSubtitle"
                    [logoText]="logoText"
                    [pendingRoute]="pendingRoute"
                    [notificationBadge]="notificationBadge"
                    [sections]="sections"
                >
                    <div fdUiSidebarFooter style="padding: var(--fd-space-xs) var(--fd-space-sm); color: var(--fd-color-text-muted);">
                        Footer slot
                    </div>
                </fd-ui-sidebar>
                <main style="padding: var(--fd-space-xl);">
                    <h2 class="fd-ui-page-title">Content area</h2>
                </main>
            </div>
        `,
    }),
};

export default meta;
type Story = StoryObj<FdUiSidebarComponent>;

export const Default: Story = {};

export const WithNotification: Story = {
    args: {
        notificationAriaLabel: 'Notifications',
        notificationHint: 'Notifications',
        notificationBadge: 4,
    },
};
