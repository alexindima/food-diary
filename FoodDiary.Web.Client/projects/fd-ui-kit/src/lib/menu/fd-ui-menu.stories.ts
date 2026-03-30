import type { Meta, StoryObj } from '@storybook/angular';
import { FdUiMenuComponent } from './fd-ui-menu.component';
import { FdUiMenuItemComponent } from './fd-ui-menu-item.component';
import { FdUiMenuDividerComponent } from './fd-ui-menu-divider.component';
import { FdUiMenuTriggerDirective } from './fd-ui-menu-trigger.directive';
import { FdUiButtonComponent } from '../button/fd-ui-button.component';
import { moduleMetadata } from '@storybook/angular';

const meta: Meta<FdUiMenuComponent> = {
    title: 'Components/Menu',
    component: FdUiMenuComponent,
    tags: ['autodocs'],
    decorators: [
        moduleMetadata({
            imports: [FdUiMenuItemComponent, FdUiMenuDividerComponent, FdUiMenuTriggerDirective, FdUiButtonComponent],
        }),
    ],
};

export default meta;
type Story = StoryObj<FdUiMenuComponent>;

export const Default: Story = {
    render: () => ({
        template: `
            <fd-ui-button variant="secondary" fill="outline" icon="more_vert" [fdUiMenuTrigger]="menu.matMenu" ariaLabel="Open menu">Options</fd-ui-button>
            <fd-ui-menu #menu>
                <fd-ui-menu-item>Edit</fd-ui-menu-item>
                <fd-ui-menu-item>Duplicate</fd-ui-menu-item>
                <fd-ui-menu-divider></fd-ui-menu-divider>
                <fd-ui-menu-item>Delete</fd-ui-menu-item>
            </fd-ui-menu>
        `,
    }),
};

export const WithIcons: Story = {
    render: () => ({
        template: `
            <fd-ui-button variant="ghost" icon="more_horiz" [fdUiMenuTrigger]="menu.matMenu" ariaLabel="Actions"></fd-ui-button>
            <fd-ui-menu #menu>
                <fd-ui-menu-item><mat-icon>edit</mat-icon> Edit</fd-ui-menu-item>
                <fd-ui-menu-item><mat-icon>content_copy</mat-icon> Duplicate</fd-ui-menu-item>
                <fd-ui-menu-item><mat-icon>share</mat-icon> Share</fd-ui-menu-item>
                <fd-ui-menu-divider></fd-ui-menu-divider>
                <fd-ui-menu-item><mat-icon>delete</mat-icon> Delete</fd-ui-menu-item>
            </fd-ui-menu>
        `,
    }),
};

export const DisabledItem: Story = {
    render: () => ({
        template: `
            <fd-ui-button variant="secondary" [fdUiMenuTrigger]="menu.matMenu">Actions</fd-ui-button>
            <fd-ui-menu #menu>
                <fd-ui-menu-item>Available action</fd-ui-menu-item>
                <fd-ui-menu-item [disabled]="true">Disabled action</fd-ui-menu-item>
                <fd-ui-menu-item>Another action</fd-ui-menu-item>
            </fd-ui-menu>
        `,
    }),
};
