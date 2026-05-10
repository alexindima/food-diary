import { CommonModule } from '@angular/common';
import {
    afterNextRender,
    booleanAttribute,
    ChangeDetectionStrategy,
    Component,
    computed,
    contentChild,
    DestroyRef,
    type ElementRef,
    inject,
    input,
    signal,
    viewChild,
    ViewEncapsulation,
} from '@angular/core';

import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import { FD_UI_DIALOG_DATA } from './fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from './fd-ui-dialog-footer.directive';
import { FdUiDialogHeaderDirective } from './fd-ui-dialog-header.directive';
import { FdUiDialogRef } from './fd-ui-dialog-ref';

let nextDialogId = 0;

export type FdUiDialogSize = 'sm' | 'md' | 'lg' | 'xl';
export type FdUiDialogBodyScrollInset = 'default' | 'edge';

export interface FdUiDialogData {
    title?: string;
    subtitle?: string;
    size?: FdUiDialogSize;
    dismissible?: boolean;
    bodyScrollInset?: FdUiDialogBodyScrollInset;
}

@Component({
    selector: 'fd-ui-dialog',
    standalone: true,
    imports: [CommonModule, FdUiIconComponent],
    templateUrl: './fd-ui-dialog.component.html',
    styleUrls: ['./fd-ui-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
})
export class FdUiDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<FdUiDialogComponent>, { optional: true });
    private readonly injectedData = inject(FD_UI_DIALOG_DATA, { optional: true }) as FdUiDialogData | null;
    private readonly destroyRef = inject(DestroyRef);

    public readonly dialogTitleId = `fd-dialog-title-${nextDialogId++}`;

    private readonly footerSlot = contentChild(FdUiDialogFooterDirective, { descendants: true });
    private readonly headerSlot = contentChild(FdUiDialogHeaderDirective, { descendants: true });
    private readonly body = viewChild<ElementRef<HTMLElement>>('body');
    private readonly isBodyScrollable = signal(false);

    public readonly title = input<string | undefined>(this.injectedData?.title);
    public readonly subtitle = input<string | undefined>(this.injectedData?.subtitle);
    public readonly size = input<FdUiDialogSize>(this.injectedData?.size ?? 'md');
    public readonly bodyScrollInset = input<FdUiDialogBodyScrollInset>(this.injectedData?.bodyScrollInset ?? 'default');
    public readonly dismissible = input(this.injectedData?.dismissible ?? true, {
        transform: booleanAttribute,
    });

    public readonly showHeader = computed(() => Boolean(this.title() || this.subtitle() || this.dismissible()));
    public readonly hasCustomHeader = computed(() => Boolean(this.headerSlot()));
    public readonly showBuiltInHeader = computed(() => this.showHeader() && !this.hasCustomHeader());
    public readonly isBodyScrollInsetVisible = computed(() => this.bodyScrollInset() === 'default' && this.isBodyScrollable());
    public readonly hasFooter = computed(() => Boolean(this.footerSlot()));

    public constructor() {
        afterNextRender(() => {
            const body = this.body()?.nativeElement;

            if (!body) {
                return;
            }

            const updateScrollableState = (): void => {
                this.isBodyScrollable.set(body.scrollHeight > body.clientHeight + 1);
            };
            const scheduleUpdate = (): void => {
                window.requestAnimationFrame(updateScrollableState);
            };
            const resizeObserver = new ResizeObserver(scheduleUpdate);
            const mutationObserver = new MutationObserver(scheduleUpdate);

            updateScrollableState();
            resizeObserver.observe(body);
            mutationObserver.observe(body, { childList: true, subtree: true, characterData: true });

            this.destroyRef.onDestroy(() => {
                resizeObserver.disconnect();
                mutationObserver.disconnect();
            });
        });
    }

    public close(result?: unknown): void {
        this.dialogRef?.close(result);
    }
}
