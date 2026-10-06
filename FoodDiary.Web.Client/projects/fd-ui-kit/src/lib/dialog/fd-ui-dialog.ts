import { CommonModule, DOCUMENT } from '@angular/common';
import {
    afterNextRender,
    afterRenderEffect,
    booleanAttribute,
    ChangeDetectionStrategy,
    Component,
    computed,
    contentChild,
    DestroyRef,
    effect,
    type ElementRef,
    inject,
    input,
    Renderer2,
    signal,
    viewChild,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { FdUiIconComponent } from '../icon/fd-ui-icon';
import { FD_UI_DIALOG_DISMISSAL_LOCK } from './fd-ui-dialog.tokens';
import { FD_UI_DIALOG_DATA } from './fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from './fd-ui-dialog-footer.directive';
import { FdUiDialogHeaderDirective } from './fd-ui-dialog-header.directive';
import { FdUiDialogRef } from './fd-ui-dialog-ref';

let nextDialogId = 0;

export type FdUiDialogSize = 'sm' | 'md' | 'lg' | 'xl';
export type FdUiDialogBodyScrollInset = 'default' | 'edge';

export type FdUiDialogData = {
    title?: string;
    subtitle?: string;
    closeAriaLabel?: string;
    size?: FdUiDialogSize;
    dismissible?: boolean;
    disableClose?: boolean;
    bodyScrollInset?: FdUiDialogBodyScrollInset;
};

@Component({
    selector: 'fd-ui-dialog',
    imports: [CommonModule, TranslatePipe, FdUiIconComponent],
    templateUrl: './fd-ui-dialog.html',
    styleUrls: ['./fd-ui-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<FdUiDialogComponent>, { optional: true });
    private readonly injectedData = inject<FdUiDialogData | null>(FD_UI_DIALOG_DATA, { optional: true });
    private readonly destroyRef = inject(DestroyRef);
    private readonly document = inject(DOCUMENT);
    private readonly renderer = inject(Renderer2);
    private readonly acquireDismissalLock = inject(FD_UI_DIALOG_DISMISSAL_LOCK, { optional: true });

    protected readonly dialogTitleId = `fd-dialog-title-${nextDialogId++}`;

    private readonly footerSlot = contentChild(FdUiDialogFooterDirective, { descendants: true });
    private readonly headerSlot = contentChild(FdUiDialogHeaderDirective, { descendants: true });
    private readonly body = viewChild<ElementRef<HTMLElement>>('body');
    private readonly root = viewChild<ElementRef<HTMLElement>>('dialogRoot');
    private readonly customHeader = viewChild<ElementRef<HTMLElement>>('customHeader');
    private readonly isBodyScrollable = signal(false);
    private containerHasExplicitName: boolean | undefined;

    protected readonly usesOverlayContainer = Boolean(this.dialogRef?.id);

    public readonly title = input<string | undefined>(this.injectedData?.title);
    public readonly subtitle = input<string | undefined>(this.injectedData?.subtitle);
    public readonly closeAriaLabel = input(this.injectedData?.closeAriaLabel ?? 'COMMON.CLOSE');
    public readonly size = input<FdUiDialogSize>(this.injectedData?.size ?? 'md');
    public readonly bodyScrollInset = input<FdUiDialogBodyScrollInset>(this.injectedData?.bodyScrollInset ?? 'default');
    public readonly dismissible = input(this.injectedData?.dismissible ?? true, {
        transform: booleanAttribute,
    });
    public readonly disableClose = input(this.injectedData?.disableClose ?? false, {
        transform: booleanAttribute,
    });

    protected readonly showHeader = computed(() => Boolean(this.title() ?? this.subtitle() ?? this.dismissible()));
    protected readonly hasCustomHeader = computed(() => Boolean(this.headerSlot()));
    protected readonly showBuiltInHeader = computed(() => this.showHeader() && !this.hasCustomHeader());
    protected readonly isBodyScrollInsetVisible = computed(() => this.bodyScrollInset() === 'default' && this.isBodyScrollable());
    protected readonly hasFooter = computed(() => Boolean(this.footerSlot()));
    protected readonly hostClass = computed(
        () =>
            `fd-ui-dialog fd-ui-dialog--size-${this.size()} fd-ui-dialog--body-scroll-${this.bodyScrollInset()}${this.isBodyScrollInsetVisible() ? ' fd-ui-dialog--body-scrollable' : ''}${this.showBuiltInHeader() || this.hasCustomHeader() ? ' fd-ui-dialog--has-header' : ''}${this.hasFooter() ? ' fd-ui-dialog--has-footer' : ''}`,
    );

    public constructor() {
        afterRenderEffect({
            write: () => {
                const heading = this.hasCustomHeader()
                    ? this.customHeader()?.nativeElement.querySelector<HTMLElement>('h1, h2, h3, h4, h5, h6, [role="heading"]')
                    : this.document.getElementById(this.dialogTitleId);
                this.syncAccessibleName(heading ?? null, this.title());
            },
        });
        effect(onCleanup => {
            if (this.disableClose()) {
                const release = this.acquireDismissalLock?.();
                if (release !== undefined) {
                    onCleanup(release);
                }
            }
        });
        afterNextRender(() => {
            const body = this.body()?.nativeElement;

            if (body === undefined) {
                return;
            }

            const updateScrollableState = (): void => {
                this.isBodyScrollable.set(body.scrollHeight > body.clientHeight + 1);
            };
            const scheduleUpdate = (): void => {
                this.document.defaultView?.requestAnimationFrame(updateScrollableState) ?? updateScrollableState();
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

    private syncAccessibleName(heading: HTMLElement | null, title: string | undefined): void {
        const container = this.usesOverlayContainer ? this.document.getElementById(this.dialogRef?.id ?? '') : this.root()?.nativeElement;
        if (container === null || container === undefined) {
            return;
        }
        if (this.hasExplicitAccessibleName(container)) {
            return;
        }
        this.writeAccessibleName(container, heading, title);
    }

    private hasExplicitAccessibleName(container: HTMLElement): boolean {
        if (!this.usesOverlayContainer) {
            return false;
        }
        this.containerHasExplicitName ??= container.hasAttribute('aria-label') || container.hasAttribute('aria-labelledby');
        return this.containerHasExplicitName;
    }

    private writeAccessibleName(container: HTMLElement, heading: HTMLElement | null, title: string | undefined): void {
        if (heading !== null) {
            const headingId = heading.id.length > 0 ? heading.id : this.dialogTitleId;
            this.renderer.setAttribute(heading, 'id', headingId);
            this.renderer.setAttribute(container, 'aria-labelledby', headingId);
            this.renderer.removeAttribute(container, 'aria-label');
        } else {
            this.renderer.removeAttribute(container, 'aria-labelledby');
            if (title !== undefined && title.length > 0) {
                this.renderer.setAttribute(container, 'aria-label', title);
            } else {
                this.renderer.removeAttribute(container, 'aria-label');
            }
        }
    }

    protected close(result?: unknown): void {
        if (!this.disableClose()) {
            this.dialogRef?.close(result);
        }
    }
}
