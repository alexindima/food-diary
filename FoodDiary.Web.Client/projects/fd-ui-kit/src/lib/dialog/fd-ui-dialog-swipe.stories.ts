import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { applicationConfig, type Meta, type StoryObj } from '@storybook/angular-vite';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiDialogShellComponent } from '../dialog-shell/fd-ui-dialog-shell';
import { FdUiDialogService } from './fd-ui-dialog.service';
import { FdUiDialogFooterDirective } from './fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from './fd-ui-dialog-ref';
import { provideFdUiDialogNavigation } from './provide-dialog-navigation';

@Component({
    selector: 'fd-dialog-swipe-example',
    imports: [FdUiButtonComponent, FdUiDialogShellComponent, FdUiDialogFooterDirective],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        @if (dialogRef) {
            <fd-ui-dialog-shell title="Protein sources" [disableClose]="locked()">
                <p>Pull the handle or header down. Content only starts a dismissal gesture when it is already at the top.</p>
                @for (row of rows; track row) {
                    <p>Food source {{ row }}: 12 g protein</p>
                }
                <div fdUiDialogFooter>
                    <fd-ui-button variant="secondary" (click)="locked.set(!locked())">
                        {{ locked() ? 'Unlock dismissal' : 'Lock dismissal' }}
                    </fd-ui-button>
                    <fd-ui-button (click)="open()">Open nested sheet</fd-ui-button>
                </div>
            </fd-ui-dialog-shell>
        } @else {
            <fd-ui-button (click)="open()">Open mobile sheet</fd-ui-button>
        }
    `,
})
class DialogSwipeExample {
    protected readonly dialogRef = inject(FdUiDialogRef, { optional: true });
    protected readonly locked = signal(false);
    protected readonly rows = Array.from({ length: 35 }, (_, index) => index + 1);
    private readonly dialogs = inject(FdUiDialogService);

    protected open(): void {
        this.dialogs.open(DialogSwipeExample, { preset: 'detail' });
    }
}

const meta: Meta<DialogSwipeExample> = {
    title: 'Behavior/Dialog swipe',
    component: DialogSwipeExample,
    decorators: [applicationConfig({ providers: [provideFdUiDialogNavigation()] })],
    tags: ['autodocs'],
    parameters: {
        docs: {
            description: {
                component:
                    'At compact widths, standard service dialogs support touch drag-to-dismiss with a decorative handle. A downward drag from the header or content already at its scroll start follows the finger. Release after 25% of the sheet height or a fast flick to dismiss; shorter drags return to rest. Native scroll gestures keep ownership until release. Controls, nested scrollers, dismissal locks, and close predicates retain their behavior. Desktop, fullscreen, surface, and standalone wrappers do not use this gesture. Back, Escape, backdrop, and the close button remain available. Reduced motion skips settling animations.',
            },
        },
    },
};

export default meta;
type Story = StoryObj<DialogSwipeExample>;
export const MobileSheet: Story = {};
