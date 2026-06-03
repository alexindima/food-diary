import type { ElementRef } from '@angular/core';
import { afterNextRender, ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import {
    FdUiButtonComponent,
    FdUiHintDirective,
    FdUiIconComponent,
    FdUiMenuComponent,
    FdUiMenuItemComponent,
    FdUiMenuTriggerDirective,
} from 'fd-ui-kit';

type HeaderOverflowAction = {
    readonly id: string;
    readonly label: string;
    readonly icon: string | null;
    readonly disabled: boolean;
    readonly target: HTMLElement;
};

const ACTION_SELECTOR = 'button, a[href], [role="button"]';

@Component({
    selector: 'fd-header-actions-overflow',
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        FdUiHintDirective,
        FdUiIconComponent,
        FdUiMenuComponent,
        FdUiMenuItemComponent,
        FdUiMenuTriggerDirective,
    ],
    templateUrl: './header-actions-overflow.html',
    styleUrl: './header-actions-overflow.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.fd-header-actions-overflow--has-overflow]': 'hasOverflow()',
        '[class.fd-header-actions-overflow--gap-sm]': 'gap() === "sm"',
    },
})
export class HeaderActionsOverflowComponent {
    private readonly actionsContainer = viewChild.required<ElementRef<HTMLElement>>('actionsContainer');
    private readonly destroyRef = inject(DestroyRef);

    public readonly gap = input<'page-header' | 'sm'>('page-header');

    protected readonly actions = signal<readonly HeaderOverflowAction[]>([]);
    protected readonly hasOverflow = computed(() => this.actions().length > 1);

    public constructor() {
        afterNextRender(() => {
            const container = this.actionsContainer().nativeElement;
            const observer = new MutationObserver(() => {
                this.refreshActions();
            });

            this.refreshActions();
            observer.observe(container, {
                attributes: true,
                attributeFilter: ['aria-disabled', 'aria-label', 'class', 'disabled', 'hidden'],
                childList: true,
                subtree: true,
            });

            this.destroyRef.onDestroy(() => {
                observer.disconnect();
            });
        });
    }

    protected activateAction(action: HeaderOverflowAction): void {
        if (action.disabled) {
            return;
        }

        action.target.click();
    }

    private refreshActions(): void {
        const container = this.actionsContainer().nativeElement;
        const targets = Array.from(container.querySelectorAll<HTMLElement>(ACTION_SELECTOR)).filter(target => {
            return this.isVisibleAction(target);
        });

        this.actions.set(targets.map((target, index) => this.createAction(target, index)));
    }

    private createAction(target: HTMLElement, index: number): HeaderOverflowAction {
        const label = this.getActionLabel(target);

        return {
            id: `${index}:${label}`,
            label,
            icon: this.getActionIcon(target),
            disabled: this.isDisabled(target),
            target,
        };
    }

    private isVisibleAction(target: HTMLElement): boolean {
        return target.hidden === false && target.closest('[hidden]') === null;
    }

    private isDisabled(target: HTMLElement): boolean {
        return target.getAttribute('aria-disabled') === 'true' || (target instanceof HTMLButtonElement && target.disabled);
    }

    private getActionLabel(target: HTMLElement): string {
        const metadata = target.closest<HTMLElement>('[data-fd-overflow-label]');
        const metadataLabel = metadata?.getAttribute('data-fd-overflow-label')?.trim();
        const ariaLabel = target.getAttribute('aria-label')?.trim();
        const textLabel = target.textContent.trim();
        const titleLabel = target.getAttribute('title')?.trim();

        return this.firstNonEmpty(metadataLabel, ariaLabel, textLabel, titleLabel) ?? '';
    }

    private getActionIcon(target: HTMLElement): string | null {
        const metadata = target.closest<HTMLElement>('[data-fd-overflow-icon]');
        const metadataIcon = metadata?.getAttribute('data-fd-overflow-icon')?.trim();
        const glyphElement = target.querySelector<HTMLElement>('.fd-ui-icon__glyph');
        const glyph = glyphElement?.textContent.trim();

        return this.firstNonEmpty(metadataIcon, glyph) ?? null;
    }

    private firstNonEmpty(...values: ReadonlyArray<string | null | undefined>): string | null {
        return values.find(value => value !== null && value !== undefined && value.length > 0) ?? null;
    }
}
