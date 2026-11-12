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
type TransferStep = 'conflict' | 'sign-in' | 'review' | 'done' | 'blocked';

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
    protected readonly step = signal<TransferStep>('conflict');
    protected readonly busy = signal(false);
    protected readonly error = signal<string | null>(null);
    protected readonly needsConfirmation = computed(() => this.error() === 'AUTH.CONFIRM.MERGE_HINT');
    protected readonly choice = signal<GoalSource | null>(null);
    protected readonly confirmationUserId = signal<UserId | null>(null);
    private readonly preview = signal<TelegramDiaryPreview | null>(null);
    protected readonly selectedGoalSource = computed<GoalSource | null>(() =>
        this.preview()?.canUseTelegramGoals === false ? 'email' : this.choice(),
    );
    protected readonly recordCounts = computed(() => {
        const counts = this.preview()?.counts;
        return counts === undefined
            ? []
            : COUNT_LABELS.flatMap(([kind, label]) => {
                  const count = counts[kind];
                  return count > 0 ? [{ label, count }] : [];
              });
    });
    protected readonly confirmLabel = computed(() =>
        this.recordCounts().length === 0 ? 'AUTH.TELEGRAM_DIARY.CONNECT' : 'AUTH.TELEGRAM_DIARY.MERGE',
    );
    protected readonly canRetryPreview = computed(
        () => this.step() === 'sign-in' && this.error() === 'AUTH.TELEGRAM_DIARY.OPERATION_PENDING',
    );
    protected readonly goalOptions = computed<Array<FdUiRadioOption<GoalSource>>>(() => {
        const preview = this.preview();
        if (preview?.canUseTelegramGoals !== true) {
            return [];
        }
        return [
            this.goalOption('email', 'AUTH.TELEGRAM_DIARY.EMAIL_GOALS', preview.emailGoals),
            this.goalOption('telegram', 'AUTH.TELEGRAM_DIARY.TELEGRAM_GOALS', preview.telegramGoals),
        ];
    });

    protected beginSignIn(): void {
        this.step.set('sign-in');
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
                    this.preview.set(preview);
                    this.step.set(useTelegramGoals === undefined ? 'review' : 'done');
                },
                error: (error: unknown) => {
                    const code = getStringProperty(getRecordProperty(error, 'error'), 'error') ?? '';
                    this.error.set(TRANSFER_ERRORS[code] ?? 'AUTH.TELEGRAM_DIARY.ERROR');
                    if (code === RESTRICTED_ACCOUNT_ERROR) {
                        this.step.set('blocked');
                    } else if (code === 'Authentication.ReauthenticationRequired') {
                        this.confirmationUserId.set(optionalEntityId<'user'>(this.auth.getUserId()));
                        this.step.set('sign-in');
                    }
                },
            });
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
