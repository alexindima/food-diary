import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { applicationConfig, type Meta, type StoryObj } from '@storybook/angular-vite';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiDialogShellComponent } from '../dialog-shell/fd-ui-dialog-shell';
import { FdUiInputComponent } from '../input/fd-ui-input';
import { FdUiDialogService } from './fd-ui-dialog.service';
import { FdUiDialogRef } from './fd-ui-dialog-ref';
import { provideFdUiDialogNavigation } from './provide-dialog-navigation';

@Component({
    selector: 'fd-dialog-navigation-example',
    imports: [FdUiButtonComponent, FdUiDialogShellComponent, FdUiInputComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        @if (dialogRef) {
            <fd-ui-dialog-shell title="Dialog navigation" [disableClose]="locked()">
                <div class="fd-stack fd-gap-md">
                    <p>Back closes only the topmost dialog. A dismissal lock keeps this dialog and its draft open.</p>
                    <fd-ui-input label="Draft" placeholder="Enter a draft" />
                    <p>Dismissal locked: {{ locked() }}</p>
                    <fd-ui-button (click)="open()">Open nested dialog</fd-ui-button>
                    <fd-ui-button (click)="locked.set(!locked())">Toggle dismissal lock</fd-ui-button>
                    <fd-ui-button (click)="dialogRef.close()">Close explicitly</fd-ui-button>
                </div>
            </fd-ui-dialog-shell>
        } @else {
            <fd-ui-button (click)="open()">Open dialog</fd-ui-button>
        }
    `,
})
class DialogNavigationExampleComponent {
    protected readonly dialogRef = inject(FdUiDialogRef, { optional: true });
    protected readonly locked = signal(false);
    private readonly dialogs = inject(FdUiDialogService);

    protected open(): void {
        this.dialogs.open(DialogNavigationExampleComponent, { preset: 'form' });
    }
}

const meta: Meta<DialogNavigationExampleComponent> = {
    title: 'Behavior/Dialog navigation',
    component: DialogNavigationExampleComponent,
    decorators: [applicationConfig({ providers: [provideFdUiDialogNavigation()] })],
    tags: ['autodocs'],
    parameters: {
        docs: {
            description: {
                component:
                    'Service-opened dialogs consume browser Back before page navigation. Try nested dialogs, a pending-operation dismissal lock, and explicit close. The URL and underlying route state stay unchanged.',
            },
        },
    },
};

export default meta;
type Story = StoryObj<DialogNavigationExampleComponent>;

export const BrowserBack: Story = {};
