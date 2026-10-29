import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { map, timer } from 'rxjs';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame';
import { NoticeBannerComponent } from '../../../../components/shared/notice-banner/notice-banner';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { PERCENT_MULTIPLIER } from '../../../../shared/lib/nutrition.constants';
import type { DashboardHydrationHistory } from '../../../../shared/models/dashboard.data';
import { HydrationDetailsDialogComponent } from '../../dialogs/hydration-details-dialog/hydration-details-dialog';
import { HYDRATION_CARD_ADD_AMOUNTS_ML, HYDRATION_CARD_FEEDBACK_MS, HYDRATION_CARD_MAX_PERCENT } from './hydration-card.config';
import { HydrationCardActionsComponent } from './hydration-card-actions/hydration-card-actions';

@Component({
    selector: 'fd-hydration-card',
    imports: [
        CommonModule,
        LocalizedNumberPipe,
        FdUiButtonComponent,
        HydrationCardActionsComponent,
        TranslatePipe,
        NoticeBannerComponent,
        DashboardWidgetFrameComponent,
    ],
    templateUrl: './hydration-card.html',
    styleUrl: './hydration-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HydrationCardComponent {
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private pendingAdd: { amount: number; previousId: string | null } | null = null;
    protected readonly language = toSignal(this.translateService.onLangChange.pipe(map(event => event.lang)), {
        initialValue: resolveTranslateLanguage(this.translateService),
    });
    public readonly total = input.required<number>();
    public readonly goal = input.required<number | null>();
    public readonly history = input<DashboardHydrationHistory | null>(null);
    public readonly isLoading = input.required<boolean>();
    public readonly canAdd = input.required<boolean>();
    public readonly addClick = output<number>();
    public readonly undoEntryId = input<string | null>(null);
    public readonly goalAction = output();

    protected readonly hasGoal = computed(() => {
        const goal = this.goal();
        return goal !== null && goal > 0;
    });
    protected readonly percent = computed(() => {
        if (!this.hasGoal()) {
            return 0;
        }
        const value = (this.total() / (this.goal() ?? 1)) * PERCENT_MULTIPLIER;
        return Math.max(0, Math.min(value, HYDRATION_CARD_MAX_PERCENT)); // allow slight overflow visualization
    });
    protected readonly fillLevel = computed(() => `${Math.min(this.percent(), PERCENT_MULTIPLIER)}%`);
    protected readonly progressValue = computed(() => Math.min(Math.max(0, this.total()), this.goal() ?? 0));
    protected readonly remaining = computed(() => Math.max(0, (this.goal() ?? 0) - this.total()));
    protected readonly isGoalReached = computed(() => this.hasGoal() && this.remaining() === 0);
    protected readonly addedAmount = signal<number | null>(null);

    public constructor() {
        effect(() => {
            const entryId = this.undoEntryId();
            const canAdd = this.canAdd();
            if (this.isLoading()) {
                return;
            }
            const pending = this.pendingAdd;
            this.pendingAdd = null;
            this.addedAmount.set(canAdd && pending !== null && entryId !== null && entryId !== pending.previousId ? pending.amount : null);
        });
        effect(onCleanup => {
            if (this.addedAmount() === null) {
                return;
            }
            const timeout = timer(HYDRATION_CARD_FEEDBACK_MS).subscribe(() => {
                this.addedAmount.set(null);
            });
            onCleanup(() => {
                timeout.unsubscribe();
            });
        });
    }

    protected onAdd(amount: number): void {
        if (!this.canAdd() || this.isLoading()) {
            return;
        }
        if (!(HYDRATION_CARD_ADD_AMOUNTS_ML as readonly number[]).includes(amount)) {
            return;
        }
        this.addedAmount.set(null);
        this.pendingAdd = { amount, previousId: this.undoEntryId() };
        this.addClick.emit(amount);
    }

    protected openDetails(): void {
        this.dialogService
            .open<HydrationDetailsDialogComponent, { total: number; history: DashboardHydrationHistory | null }, boolean>(
                HydrationDetailsDialogComponent,
                {
                    size: 'sm',
                    data: { total: this.total(), history: this.history() },
                },
            )
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(editGoal => {
                if (editGoal === true) {
                    this.goalAction.emit();
                }
            });
    }

    protected onGoalAction(): void {
        this.goalAction.emit();
    }
}
