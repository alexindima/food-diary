import { signal } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular-vite';

import { FdUiTabsComponent } from './fd-ui-tabs';

const meta: Meta<FdUiTabsComponent> = {
    title: 'Components/Tabs',
    component: FdUiTabsComponent,
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<FdUiTabsComponent>;

export const Default: Story = {
    render: () => {
        const selected = signal('overview');
        return {
            props: {
                tabs: [
                    { value: 'overview', label: 'Overview' },
                    { value: 'nutrition', label: 'Nutrition' },
                    { value: 'history', label: 'History' },
                ],
                selected,
            },
            template: `
                <fd-ui-tabs #tabSet ariaLabel="Food details" [tabs]="tabs" [(selectedValue)]="selected" />
    @for (tab of tabs; track tab.value) {
        <div role="tabpanel" tabindex="0" [id]="tabSet.panelId(tab.value)" [attr.aria-labelledby]="tabSet.tabId(tab.value)" [hidden]="selected() !== tab.value">
            {{ tab.label }} content
        </div>
    }

            `,
        };
    },
};

export const ManyTabs: Story = {
    render: () => {
        const selected = signal('mon');
        return {
            props: {
                tabs: [
                    { value: 'mon', label: 'Mon' },
                    { value: 'tue', label: 'Tue' },
                    { value: 'wed', label: 'Wed' },
                    { value: 'thu', label: 'Thu' },
                    { value: 'fri', label: 'Fri' },
                    { value: 'sat', label: 'Sat' },
                    { value: 'sun', label: 'Sun' },
                ],
                selected,
            },
            template: `<fd-ui-tabs #tabSet ariaLabel="Food details" [tabs]="tabs" [(selectedValue)]="selected" />
    @for (tab of tabs; track tab.value) {
        <div role="tabpanel" tabindex="0" [id]="tabSet.panelId(tab.value)" [attr.aria-labelledby]="tabSet.tabId(tab.value)" [hidden]="selected() !== tab.value">
            {{ tab.label }} content
        </div>
    }`,
        };
    },
};

export const TwoTabs: Story = {
    render: () => {
        const selected = signal('products');
        return {
            props: {
                tabs: [
                    { value: 'products', label: 'Products' },
                    { value: 'recipes', label: 'Recipes' },
                ],
                selected,
            },
            template: `<fd-ui-tabs #tabSet ariaLabel="Food details" [tabs]="tabs" [(selectedValue)]="selected" />
    @for (tab of tabs; track tab.value) {
        <div role="tabpanel" tabindex="0" [id]="tabSet.panelId(tab.value)" [attr.aria-labelledby]="tabSet.tabId(tab.value)" [hidden]="selected() !== tab.value">
            {{ tab.label }} content
        </div>
    }`,
        };
    },
};

export const Selection: Story = {
    args: {
        mode: 'selection',
        ariaLabel: 'Period',
        selectedValue: 'week',
        tabs: [
            { value: 'week', label: 'Week' },
            { value: 'month', label: 'Month' },
        ],
    },
};
