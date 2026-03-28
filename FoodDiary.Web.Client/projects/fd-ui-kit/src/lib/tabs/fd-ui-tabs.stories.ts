import type { Meta, StoryObj } from '@storybook/angular';
import { signal } from '@angular/core';
import { FdUiTabsComponent } from './fd-ui-tabs.component';

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
                <fd-ui-tabs [tabs]="tabs" [(selectedValue)]="selected"></fd-ui-tabs>
                <p style="margin-top: 16px; color: #666;">Selected tab: {{ selected() }}</p>
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
            template: '<fd-ui-tabs [tabs]="tabs" [(selectedValue)]="selected"></fd-ui-tabs>',
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
            template: '<fd-ui-tabs [tabs]="tabs" [(selectedValue)]="selected"></fd-ui-tabs>',
        };
    },
};
