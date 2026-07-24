import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, maxLength, required } from '@angular/forms/signals';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';

import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import type { AttentionSignal, AttentionSignalSettings, ClientSummary } from '../../../../shared/models/dietologist.data';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { DietologistFacade } from '../../lib/dietologist.facade';
import { buildClientCardViewModels } from './dietologist-clients-lib/dietologist-clients.mapper';
import type { ClientCardViewModel } from './dietologist-clients-lib/dietologist-clients.types';
import { DietologistClientsListComponent } from './dietologist-clients-list/dietologist-clients-list';
import { DIETOLOGIST_CLIENTS_TOUR } from './dietologist-clients-tour';

const BULK_RECOMMENDATION_MAX_LENGTH = 2000;
const ATTENTION_SNOOZE_DAYS = 7;
const HOURS_PER_DAY = 24;
const MINUTES_PER_HOUR = 60;
const SECONDS_PER_MINUTE = 60;
const MILLISECONDS_PER_SECOND = 1000;
const MILLISECONDS_PER_DAY = HOURS_PER_DAY * MINUTES_PER_HOUR * SECONDS_PER_MINUTE * MILLISECONDS_PER_SECOND;

@Component({
    selector: 'fd-dietologist-clients-page',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FormField,
        FormRoot,
        DatePipe,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiTextareaComponent,
        DietologistClientsListComponent,
    ],
    templateUrl: './dietologist-clients-page.html',
    styleUrls: ['./dietologist-clients-page.scss'],
})
export class DietologistClientsPageComponent {
    private readonly dietologistFacade = inject(DietologistFacade);
    private readonly router = inject(Router);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly toastService = inject(FdUiToastService);
    private readonly languageVersion = signal(0);

    protected readonly clients = signal<ClientSummary[]>([]);
    protected readonly loading = signal(true);
    protected readonly loadError = signal(false);
    protected readonly attentionSignals = signal<AttentionSignal[]>([]);
    protected readonly attentionLoading = signal(true);
    protected readonly attentionError = signal(false);
    protected readonly attentionSettings = signal<AttentionSignalSettings>({
        inactivityDays: 3,
        calorieDeviationPercent: 25,
        sustainedDays: 3,
        weightChangePercent: 3,
        lookbackDays: 14,
    });
    protected readonly selectedClientIds = signal<ReadonlySet<string>>(new Set<string>());
    protected readonly bulkSending = signal(false);
    private readonly bulkIdempotencyKey = signal<string | null>(null);
    protected readonly bulkModel = signal({ text: '' });
    protected readonly bulkForm = form(this.bulkModel, path => {
        required(path.text);
        maxLength(path.text, BULK_RECOMMENDATION_MAX_LENGTH);
    });
    protected readonly clientItems = computed<ClientCardViewModel[]>(() => {
        this.languageVersion();
        return buildClientCardViewModels(this.clients(), resolveTranslateLanguage(this.translateService));
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });

        this.loadClients();
        this.loadAttentionSignals();
    }

    protected retryLoad(): void {
        this.loadClients();
        this.loadAttentionSignals();
    }

    protected updateAttentionSetting(key: keyof AttentionSignalSettings, rawValue: string): void {
        const value = Number(rawValue);
        if (Number.isFinite(value) && value > 0) {
            this.attentionSettings.update(settings => ({ ...settings, [key]: value }));
        }
    }

    protected applyAttentionSettings(): void {
        this.loadAttentionSignals();
    }

    protected acknowledgeSignal(attentionSignal: AttentionSignal): void {
        this.setAttentionState(attentionSignal, 'Acknowledge');
    }

    protected snoozeSignal(attentionSignal: AttentionSignal): void {
        const snoozedUntilUtc = new Date(Date.now() + ATTENTION_SNOOZE_DAYS * MILLISECONDS_PER_DAY).toISOString();
        this.setAttentionState(attentionSignal, 'Snooze', snoozedUntilUtc);
    }

    protected toggleClientSelection(clientId: string, selected: boolean): void {
        this.selectedClientIds.update(ids => {
            const next = new Set(ids);
            if (selected) {
                next.add(clientId);
            } else {
                next.delete(clientId);
            }

            return next;
        });
    }

    protected sendBulkRecommendation(): void {
        const clientIds = [...this.selectedClientIds()];
        const text = this.bulkModel().text.trim();
        if (clientIds.length === 0 || text.length === 0 || this.bulkSending()) {
            this.bulkForm().markAsTouched();
            return;
        }

        this.dialogService
            .open(FdUiConfirmDialogComponent, {
                preset: 'confirm',
                data: {
                    title: this.translateService.instant('BULK_RECOMMENDATIONS.CONFIRM_TITLE'),
                    message: this.translateService.instant('BULK_RECOMMENDATIONS.CONFIRM_MESSAGE', { count: clientIds.length }),
                    confirmLabel: this.translateService.instant('BULK_RECOMMENDATIONS.CONFIRM'),
                    cancelLabel: this.translateService.instant('COMMON.CANCEL'),
                },
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(confirmed => {
                if (confirmed === true) {
                    this.executeBulkRecommendation(clientIds, text);
                }
            });
    }

    private loadClients(): void {
        this.loading.set(true);
        this.loadError.set(false);
        this.dietologistFacade
            .getMyClients()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: clients => {
                    this.clients.set(clients);
                    this.loading.set(false);
                },
                error: () => {
                    this.clients.set([]);
                    this.loading.set(false);
                    this.loadError.set(true);
                },
            });
    }

    private loadAttentionSignals(): void {
        this.attentionLoading.set(true);
        this.attentionError.set(false);
        this.dietologistFacade
            .getAttentionSignals(this.attentionSettings())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: signals => {
                    this.attentionSignals.set(signals);
                    this.attentionLoading.set(false);
                },
                error: () => {
                    this.attentionSignals.set([]);
                    this.attentionLoading.set(false);
                    this.attentionError.set(true);
                },
            });
    }

    private setAttentionState(
        attentionSignal: AttentionSignal,
        action: 'Acknowledge' | 'Snooze',
        snoozedUntilUtc: string | null = null,
    ): void {
        this.dietologistFacade
            .setAttentionSignalState(attentionSignal, action, snoozedUntilUtc)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.attentionSignals.update(signals => signals.filter(item => item.id !== attentionSignal.id));
                    this.toastService.success(
                        this.translateService.instant(action === 'Snooze' ? 'ATTENTION.SNOOZE_SUCCESS' : 'ATTENTION.ACKNOWLEDGE_SUCCESS'),
                    );
                },
                error: () => this.toastService.error(this.translateService.instant('ATTENTION.ACTION_ERROR')),
            });
    }

    private executeBulkRecommendation(clientIds: string[], text: string): void {
        const idempotencyKey = this.bulkIdempotencyKey() ?? crypto.randomUUID();
        this.bulkIdempotencyKey.set(idempotencyKey);
        this.bulkSending.set(true);
        this.dietologistFacade
            .bulkCreateRecommendations(clientIds, text, idempotencyKey)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: result => {
                    const failedCount = result.recipients.filter(recipient => !recipient.succeeded).length;
                    this.bulkSending.set(false);
                    this.bulkIdempotencyKey.set(null);
                    this.selectedClientIds.set(new Set<string>());
                    this.bulkModel.set({ text: '' });
                    if (failedCount === 0) {
                        this.toastService.success(
                            this.translateService.instant('BULK_RECOMMENDATIONS.SUCCESS', { count: result.recipients.length }),
                        );
                    } else {
                        this.toastService.error(
                            this.translateService.instant('BULK_RECOMMENDATIONS.PARTIAL_ERROR', {
                                failed: failedCount,
                                total: result.recipients.length,
                            }),
                        );
                    }
                },
                error: () => {
                    this.bulkSending.set(false);
                    this.toastService.error(this.translateService.instant('BULK_RECOMMENDATIONS.ERROR'));
                },
            });
    }

    protected openClient(client: ClientSummary): void {
        void this.router.navigate(['/dietologist', 'clients', client.userId]);
    }

    protected startDietologistClientsTour(force = true): void {
        this.tourService.start(this.localizedTour.build(DIETOLOGIST_CLIENTS_TOUR), { force });
    }
}
