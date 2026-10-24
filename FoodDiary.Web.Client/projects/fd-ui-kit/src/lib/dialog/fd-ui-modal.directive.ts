import { DestroyRef, Directive, effect, inject, input, output, TemplateRef, untracked, ViewContainerRef } from '@angular/core';

import { FdUiDialogService } from './fd-ui-dialog.service';
import type { FdUiDialogRef } from './fd-ui-dialog-ref';

/** Presents an existing sheet template through the same modal lifecycle as service dialogs. */
@Directive({ selector: 'ng-template[fdUiModal]' })
export class FdUiModalDirective {
    public readonly fdUiModal = input.required<boolean>();
    public readonly fdUiModalDisableClose = input(false);
    public readonly fdUiModalLabel = input<string>();
    public readonly fdUiModalLabelledBy = input<string>();
    public readonly fdUiModalClosed = output();

    private readonly template = inject<TemplateRef<unknown>>(TemplateRef);
    private readonly viewContainerRef = inject(ViewContainerRef);
    private readonly dialogs = inject(FdUiDialogService);
    private ref: FdUiDialogRef | undefined;
    private destroyed = false;

    public constructor() {
        effect(() => {
            const open = this.fdUiModal();
            const disableClose = this.fdUiModalDisableClose();
            const ariaLabel = this.fdUiModalLabel();
            const ariaLabelledBy = this.fdUiModalLabelledBy();
            untracked(() => {
                if (open && this.ref === undefined) {
                    this.ref = this.dialogs.open(this.template, {
                        preset: 'surface',
                        width: '100vw',
                        height: '100dvh',
                        maxWidth: '100vw',
                        maxHeight: '100dvh',
                        hasBackdrop: false,
                        ariaModal: true,
                        ariaLabel,
                        ariaLabelledBy,
                        disableClose,
                        viewContainerRef: this.viewContainerRef,
                    });
                    this.ref.closed.subscribe(() => {
                        this.ref = undefined;
                        if (!this.destroyed) {
                            this.fdUiModalClosed.emit();
                        }
                    });
                } else if (!open) {
                    this.ref?.close();
                }
                if (this.ref !== undefined) {
                    this.ref.disableClose = disableClose;
                }
            });
        });
        inject(DestroyRef).onDestroy(() => {
            this.destroyed = true;
            this.ref?.close();
        });
    }
}
