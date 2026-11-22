import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiRadioGroupComponent, type FdUiRadioOption } from 'fd-ui-kit/radio/fd-ui-radio-group';
import { finalize } from 'rxjs';

import { AuthService } from '../../../../services/auth.service';
import type { TelegramDiaryGoals, TelegramDiaryPreview } from '../../../../shared/auth/telegram-diary.data';
import { getRecordProperty, getStringProperty } from '../../../../shared/lib/unknown-value.utils';
import { optionalEntityId, type UserId } from '../../../../shared/models/semantics/entity-id';
import { AuthComponent } from '../../components/auth/auth';

type GoalSource = 'email' | 'telegram';
type TransferState =
    | { kind: 'conflict' | 'sign-in' | 'done' | 'blocked'; preview: TelegramDiaryPreview | null }
    | { kind: 'review'; preview: TelegramDiaryPreview };

const RESTRICTED_ACCOUNT_ERROR = 'User.TelegramDiaryRestrictedAccount';

const COUNT_LABELS = [
    ['meals', 'AUTH.TELEGRAM_DIARY.MEALS'],
    ['recipes', 'AUTH.TELEGRAM_DIARY.RECIPES'],
    ['products', 'AUTH.TELEGRAM_DIARY.PRODUCTS'],
    ['waterEntries', 'AUTH.TELEGRAM_DIARY.WATER_ENTRIES'],
    ['measurements', 'AUTH.TELEGRAM_DIARY.MEASUREMENTS'],
] as const satisfies ReadonlyArray<readonly [keyof TelegramDiaryPreview['counts'], string]>;

const TRANSFER_ERRORS: Readonly<Record<string, string>> = {
    'Authentication.TelegramDiaryTargetSignInRequired': 'AUTH.TELEGRAM_DIARY.WRONG_ACCOUNT',
    'Authentication.TelegramDiaryProofExpired': 'AUTH.TELEGRAM_DIARY.EXPIRED',
    'Authentication.TelegramDiaryOperationPending': 'AUTH.TELEGRAM_DIARY.OPERATION_PENDING',
    'User.TelegramDiaryTransferUnavailable': 'AUTH.TELEGRAM_DIARY.UNAVAILABLE',
    [RESTRICTED_ACCOUNT_ERROR]: 'AUTH.TELEGRAM_DIARY.RESTRICTED_ACCOUNT',
    'Authentication.ReauthenticationRequired': 'AUTH.CONFIRM.MERGE_HINT',
    'User.TelegramDiaryMeasurementConflict': 'AUTH.TELEGRAM_DIARY.MEASUREMENT_CONFLICT',
    'User.TelegramDiaryGoalsUnavailable': 'AUTH.TELEGRAM_DIARY.GOALS_UNAVAILABLE',
};

@Component({
    selector: 'fd-telegram-diary-transfer',
    imports: [AuthComponent, FdUiButtonComponent, FdUiRadioGroupComponent, TranslatePipe],
    templateUrl: './telegram-diary-transfer.html',
    styleUrl: './telegram-diary-transfer.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TelegramDiaryTransferComponent {
    public readonly ticket = input.required<string>();
    public readonly keepSeparate = output();
    public readonly useAnotherEmail = output();

    private readonly auth = inject(AuthService);
    private readonly translate = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly transfer = signal<TransferState>({ kind: 'conflict', preview: null });
    protected readonly step = computed(() => this.transfer().kind);
    protected readonly busy = signal(false);
    protected readonly error = signal<string | null>(null);
    protected readonly needsConfirmation = computed(() => this.error() === 'AUTH.CONFIRM.MERGE_HINT');
    protected readonly choice = signal<GoalSource | null>(null);
    protected readonly confirmationUserId = signal<UserId | null>(null);
    protected readonly selectedGoalSource = computed<GoalSource | null>(() =>
        this.transfer().preview?.canUseTelegramGoals === false ? 'email' : this.choice(),
    );
    protected readonly canRetryPreview = computed(
        () => this.step() === 'sign-in' && this.error() === 'AUTH.TELEGRAM_DIARY.OPERATION_PENDING',
    );
    protected readonly transferView = computed(() => {
        const step = this.transfer();
        if (step.kind !== 'review') {
            return step;
        }
        const counts = step.preview.counts;
        const recordCounts = COUNT_LABELS.flatMap(([kind, label]) => {
            const count = counts[kind];
            return count > 0 ? [{ label, count }] : [];
        });
        return {
            ...step,
            recordCounts,
            confirmLabel: recordCounts.length === 0 ? 'AUTH.TELEGRAM_DIARY.CONNECT' : 'AUTH.TELEGRAM_DIARY.MERGE',
            goalOptions: this.getGoalOptions(step.preview),
        };
    });

    protected beginSignIn(): void {
        this.transfer.update(({ preview }) => ({ kind: 'sign-in', preview }));
    }

    protected loadPreview(): void {
        this.runTransfer();
    }

    protected confirm(): void {
        const choice = this.selectedGoalSource();
        if (choice !== null) {
            this.runTransfer(choice === 'telegram');
        }
    }

    protected onKeepSeparate(): void {
        if (this.busy() || this.step() === 'done') {
            return;
        }
        this.keepSeparate.emit();
    }

    protected continueToSignIn(): void {
        void this.auth.onLogoutAsync(true);
    }

    private runTransfer(useTelegramGoals?: boolean): void {
        if (this.busy()) {
            return;
        }
        this.busy.set(true);
        this.error.set(null);
        this.auth
            .transferTelegramDiary(this.ticket(), useTelegramGoals)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.busy.set(false);
                }),
            )
            .subscribe({
                next: preview => {
                    this.transfer.set({ kind: useTelegramGoals === undefined ? 'review' : 'done', preview });
                },
                error: (error: unknown) => {
                    const code = getStringProperty(getRecordProperty(error, 'error'), 'error') ?? '';
                    this.error.set(TRANSFER_ERRORS[code] ?? 'AUTH.TELEGRAM_DIARY.ERROR');
                    if (code === RESTRICTED_ACCOUNT_ERROR) {
                        this.transfer.update(({ preview }) => ({ kind: 'blocked', preview }));
                    } else if (code === 'Authentication.ReauthenticationRequired') {
                        this.confirmationUserId.set(optionalEntityId<'user'>(this.auth.getUserId()));
                        this.transfer.update(({ preview }) => ({ kind: 'sign-in', preview }));
                    }
                },
            });
    }

    private getGoalOptions(preview: TelegramDiaryPreview): Array<FdUiRadioOption<GoalSource>> {
        const canUseTelegramGoals: unknown = preview.canUseTelegramGoals;
        if (canUseTelegramGoals !== true) {
            return [];
        }
        return [
            this.goalOption('email', 'AUTH.TELEGRAM_DIARY.EMAIL_GOALS', preview.emailGoals),
            this.goalOption('telegram', 'AUTH.TELEGRAM_DIARY.TELEGRAM_GOALS', preview.telegramGoals),
        ];
    }

    private goalOption(value: GoalSource, key: string, goals: TelegramDiaryGoals): FdUiRadioOption<GoalSource> {
        const { weight, waist } = goals;
        const displayGoals = { ...goals, weight: weight === 0 ? null : weight, waist: waist === 0 ? null : waist };
        const summary = Object.fromEntries(Object.entries(displayGoals).map(([name, goal]) => [name, goal ?? '–']));
        return {
            value,
            label: this.translate.instant(key),
            description: this.translate.instant('AUTH.TELEGRAM_DIARY.GOAL_SUMMARY', summary),
        };
    }
}
