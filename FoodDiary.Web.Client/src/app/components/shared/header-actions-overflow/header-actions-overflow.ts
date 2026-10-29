import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    ElementRef,
    inject,
    input,
    signal,
    viewChild,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourTargetRegistry } from 'fd-tour';
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
    readonly disabledReason: string | null;
    readonly target: HTMLElement;
    readonly primary: boolean;
    readonly tourId: string | null;
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
    private readonly tourTargets = inject(FdTourTargetRegistry);
    private readonly overflowTrigger = viewChild<ElementRef<HTMLElement>, ElementRef<HTMLElement>>('overflowTrigger', { read: ElementRef });
    private readonly menuTrigger = viewChild<FdUiMenuTriggerDirective, FdUiMenuTriggerDirective>('overflowTrigger', {
        read: FdUiMenuTriggerDirective,
    });

    public readonly gap = input<'page-header' | 'sm'>('page-header');

    protected readonly actions = signal<readonly HeaderOverflowAction[]>([]);
    protected readonly hasOverflow = computed(() => this.actions().length > 1);
    protected readonly primaryAction = computed(() => this.actions().find(action => action.primary));
    protected readonly menuActions = computed(() => this.actions().filter(action => action !== this.primaryAction()));

    public constructor() {
        this.destroyRef.onDestroy(
            this.tourTargets.register({
                canReveal: selector => this.canRevealTourTarget(selector),
                reveal: () => {
                    const origin = this.overflowTrigger()?.nativeElement;
                    if (typeof origin?.scrollIntoView === 'function') {
                        origin.scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: 'instant' });
                    }
                    const trigger = this.menuTrigger();
                    const wasOpen = trigger?.isOpen() === true;
                    trigger?.open('none');
                    return (): void => {
                        if (!wasOpen) {
                            trigger?.close(false);
                        }
                    };
                },
            }),
        );
        afterNextRender(() => {
            const container = this.actionsContainer().nativeElement;
            const observer = new MutationObserver(() => {
                this.refreshActions();
            });

            this.refreshActions();
            observer.observe(container, {
                attributes: true,
                attributeFilter: [
                    'data-fd-overflow-primary',
                    'data-fd-overflow-label',
                    'data-fd-overflow-icon',
                    'data-tour-id',
                    'aria-disabled',
                    'aria-label',
                    'class',
                    'disabled',
                    'hidden',
                    'title',
                ],
                characterData: true,
                childList: true,
                subtree: true,
            });

            this.destroyRef.onDestroy(() => {
                observer.disconnect();
            });
        });
    }

    protected activateAction(action: HeaderOverflowAction, menuEvent?: Event): void {
        if (action.disabled) {
            return;
        }

        if (menuEvent !== undefined) {
            // This container closes the menu before dispatching the original action.
            // Prevent the old item from closing a new menu opened by that action.
            menuEvent.preventDefault();
            this.overflowTrigger()?.nativeElement.querySelector<HTMLButtonElement>('button')?.focus();
            this.menuTrigger()?.close(false);
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
            disabledReason: this.getDisabledReason(target),
            target,
            primary: target.closest('[data-fd-overflow-primary]') !== null,
            tourId: target.closest('[data-tour-id]')?.getAttribute('data-tour-id') ?? null,
        };
    }

    private canRevealTourTarget(selector: string): boolean {
        const trigger = this.overflowTrigger()?.nativeElement;
        const style = trigger?.ownerDocument.defaultView?.getComputedStyle(trigger);
        if (trigger === undefined || style?.display === 'none' || style?.visibility === 'hidden') {
            return false;
        }
        try {
            return this.menuActions().some(action => {
                const target = action.target.closest('[data-tour-id]');
                return target?.matches(selector) === true;
            });
        } catch {
            return false;
        }
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
        const textContent = target.cloneNode(true);
        if (textContent instanceof HTMLElement) {
            for (const decorative of textContent.querySelectorAll('[aria-hidden="true"], [hidden], .fd-ui-icon__glyph')) {
                decorative.remove();
            }
        }
        const textLabel = textContent.textContent?.trim();
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

    private getDisabledReason(target: HTMLElement): string | null {
        if (!this.isDisabled(target)) {
            return null;
        }

        return this.firstNonEmpty(
            target.getAttribute('title')?.trim(),
            target.closest<HTMLElement>('[title]')?.getAttribute('title')?.trim(),
        );
    }

    private firstNonEmpty(...values: ReadonlyArray<string | null | undefined>): string | null {
        return values.find(value => value !== null && value !== undefined && value.length > 0) ?? null;
    }
}
