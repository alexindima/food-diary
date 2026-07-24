import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, maxLength, required } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { catchError, firstValueFrom, forkJoin, type Observable, of, switchMap, tap } from 'rxjs';

import { LocalizedDatePipe } from '../../../../shared/i18n/localized-date.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateInputValue, parseLocalDateInputValue } from '../../../../shared/lib/local-date.utils';
import type {
    ClientSummary,
    ClientTask,
    DietologistClientGoals,
    DietologistRecommendation,
    RecommendationTemplate,
} from '../../../../shared/models/dietologist.data';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import type { DashboardSnapshot } from '../../../dashboard/models/dashboard.data';
import { RecommendationThreadComponent } from '../../../recommendations/components/recommendation-thread/recommendation-thread';
import { DietologistFacade } from '../../lib/dietologist.facade';
import {
    buildBodyTiles,
    buildClientDashboardSections,
    buildClientProfileChips,
    buildClientProfileDetails,
    buildFastingView,
    buildGoalTiles,
    buildHydrationView,
    buildMealViews,
    buildNutritionTiles,
    buildRecommendationViews,
    buildWaistView,
    buildWeightView,
    type ClientBodyMeasurementView,
    type ClientDashboardSection,
    type ClientFastingView,
    type ClientHydrationView,
    type ClientMealView,
    type ClientMetricTile,
    type ClientProfileDetail,
    type ClientRecommendationView,
    getClientDashboardTitle,
} from './client-dashboard-lib/client-dashboard.mapper';
import { CLIENT_DASHBOARD_TOUR } from './client-dashboard-tour';
import { ClientDashboardFastingCardComponent } from './components/client-dashboard-fasting-card';
import { ClientDashboardHeaderComponent } from './components/client-dashboard-header';
import { ClientDashboardHydrationCardComponent } from './components/client-dashboard-hydration-card';
import { ClientDashboardMealsCardComponent } from './components/client-dashboard-meals-card';
import { ClientDashboardMetricListComponent } from './components/client-dashboard-metric-list';
import { ClientDashboardNoticesComponent } from './components/client-dashboard-notices';
import { ClientDashboardSummaryCardComponent } from './components/client-dashboard-summary-card';

const RECOMMENDATION_MAX_LENGTH = 2000;
const TASK_TITLE_MAX_LENGTH = 200;
const TASK_DETAILS_MAX_LENGTH = 2000;
const CLIENT_DASHBOARD_TREND_DAYS = 14;
const DEFAULT_PERIOD_DAYS = 7;
const HOURS_PER_DAY = 24;
const MINUTES_PER_HOUR = 60;
const SECONDS_PER_MINUTE = 60;
const MILLISECONDS_PER_SECOND = 1000;
const MILLISECONDS_PER_DAY = HOURS_PER_DAY * MINUTES_PER_HOUR * SECONDS_PER_MINUTE * MILLISECONDS_PER_SECOND;
const PERIOD_PRESET_DAYS = {
    week: 7,
    twoWeeks: 14,
    month: 30,
} as const;

type DateFilterFormModel = {
    dateFrom: string;
    dateTo: string;
};

type RecommendationFormModel = {
    text: string;
    templateName: string;
};

type TaskFormModel = {
    title: string;
    details: string;
    dueDate: string;
};

@Component({
    selector: 'fd-client-dashboard',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        LocalizedDatePipe,
        FormField,
        FormRoot,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiDateInputComponent,
        FdUiInputComponent,
        FdUiTextareaComponent,
        ClientDashboardHeaderComponent,
        ClientDashboardNoticesComponent,
        ClientDashboardMetricListComponent,
        ClientDashboardMealsCardComponent,
        ClientDashboardSummaryCardComponent,
        ClientDashboardHydrationCardComponent,
        ClientDashboardFastingCardComponent,
        RecommendationThreadComponent,
    ],
    templateUrl: './client-dashboard.html',
    styleUrls: ['./client-dashboard.scss'],
})
export class ClientDashboardComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly dietologistFacade = inject(DietologistFacade);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);

    protected readonly periodPresetDays = PERIOD_PRESET_DAYS;
    protected readonly client = signal<ClientSummary | null>(null);
    protected readonly dashboard = signal<DashboardSnapshot | null>(null);
    protected readonly goals = signal<DietologistClientGoals | null>(null);
    protected readonly recommendations = signal<DietologistRecommendation[]>([]);
    protected readonly recommendationTemplates = signal<RecommendationTemplate[]>([]);
    protected readonly tasks = signal<ClientTask[]>([]);
    protected readonly loading = signal(true);
    protected readonly detailsLoading = signal(false);
    protected readonly savingRecommendation = signal(false);
    protected readonly savingTemplate = signal(false);
    protected readonly savingTask = signal(false);
    protected readonly changingTaskIds = signal<ReadonlySet<string>>(new Set<string>());
    protected readonly openDiscussionId = signal<string | null>(null);
    protected readonly error = signal<string | null>(null);
    protected readonly sectionLoadError = signal<string | null>(null);
    protected readonly selectedDateTo = signal(formatDateInputValue(new Date()));
    protected readonly selectedDateFrom = signal(formatDateInputValue(this.addDays(new Date(), -(DEFAULT_PERIOD_DAYS - 1))));
    protected readonly dateFilterModel = signal<DateFilterFormModel>({
        dateFrom: this.selectedDateFrom(),
        dateTo: this.selectedDateTo(),
    });
    private readonly submitDateFilterFormAsync = async (): Promise<void> => {
        await this.applyDateFilterAsync();
    };
    protected readonly dateFilterForm = form(
        this.dateFilterModel,
        path => {
            required(path.dateFrom);
            required(path.dateTo);
        },
        {
            submission: {
                action: this.submitDateFilterFormAsync,
            },
        },
    );

    protected toggleDiscussion(recommendationId: string): void {
        this.openDiscussionId.update(current => (current === recommendationId ? null : recommendationId));
    }

    protected readonly recommendationModel = signal<RecommendationFormModel>({ text: '', templateName: '' });
    private readonly submitRecommendationFormAsync = async (): Promise<void> => {
        await this.submitRecommendationAsync();
    };
    protected readonly recommendationForm = form(
        this.recommendationModel,
        path => {
            required(path.text);
            maxLength(path.text, RECOMMENDATION_MAX_LENGTH);
        },
        {
            submission: {
                action: this.submitRecommendationFormAsync,
            },
        },
    );
    protected readonly taskModel = signal<TaskFormModel>({ title: '', details: '', dueDate: '' });
    private readonly submitTaskFormAsync = async (): Promise<void> => {
        await this.submitTaskAsync();
    };
    protected readonly taskForm = form(
        this.taskModel,
        path => {
            required(path.title);
            maxLength(path.title, TASK_TITLE_MAX_LENGTH);
            maxLength(path.details, TASK_DETAILS_MAX_LENGTH);
        },
        {
            submission: {
                action: this.submitTaskFormAsync,
            },
        },
    );
    protected readonly clientTitle = computed(() => {
        const client = this.client();
        if (client === null) {
            return '';
        }

        return getClientDashboardTitle(client);
    });
    protected readonly profileChips = computed(() => {
        const client = this.client();
        return buildClientProfileChips(client).map(value => this.localizeProfileValue(value, client));
    });
    protected readonly profileDetails = computed<ClientProfileDetail[]>(() => {
        const client = this.client();
        return buildClientProfileDetails(client).map(detail => ({
            ...detail,
            value: this.localizeProfileValue(detail.value, client),
        }));
    });
    protected readonly visibleSections = computed<ClientDashboardSection[]>(() => {
        return buildClientDashboardSections(this.client());
    });
    protected readonly hasAnyPermission = computed(() => {
        return this.visibleSections().length > 0;
    });
    protected readonly hasPeriodFilterPermission = computed(() => {
        const client = this.client();
        return client !== null && this.shouldLoadDashboardSnapshot(client);
    });
    protected readonly nutritionTiles = computed<ClientMetricTile[]>(() =>
        this.client()?.permissions.shareStatistics === true ? buildNutritionTiles(this.dashboard()) : [],
    );
    protected readonly bodyTiles = computed<ClientMetricTile[]>(() => buildBodyTiles(this.dashboard(), this.client()?.permissions));
    protected readonly goalTiles = computed<ClientMetricTile[]>(() => buildGoalTiles(this.goals()));
    protected readonly mealItems = computed<ClientMealView[]>(() =>
        this.client()?.permissions.shareMeals === true ? buildMealViews(this.dashboard()) : [],
    );
    protected readonly weightSummary = computed<ClientBodyMeasurementView | null>(() =>
        this.client()?.permissions.shareWeight === true ? buildWeightView(this.dashboard()) : null,
    );
    protected readonly waistSummary = computed<ClientBodyMeasurementView | null>(() =>
        this.client()?.permissions.shareWaist === true ? buildWaistView(this.dashboard()) : null,
    );
    protected readonly hydrationSummary = computed<ClientHydrationView | null>(() =>
        this.client()?.permissions.shareHydration === true ? buildHydrationView(this.dashboard()) : null,
    );
    protected readonly fastingSummary = computed<ClientFastingView | null>(() =>
        this.client()?.permissions.shareFasting === true ? buildFastingView(this.dashboard()) : null,
    );
    protected readonly recommendationItems = computed<ClientRecommendationView[]>(() => buildRecommendationViews(this.recommendations()));

    public constructor() {
        const clientId = this.route.snapshot.paramMap.get('clientId');
        this.openDiscussionId.set(this.route.snapshot.queryParamMap.get('recommendationId'));
        this.dietologistFacade
            .getMyClients()
            .pipe(
                switchMap(clients => {
                    const found = clients.find(c => c.userId === clientId) ?? null;
                    this.client.set(found);
                    this.loading.set(false);

                    if (found === null) {
                        return of(null);
                    }

                    this.detailsLoading.set(true);
                    return this.loadClientDetails(found);
                }),
            )
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.detailsLoading.set(false);
                },
                error: () => {
                    this.loading.set(false);
                    this.detailsLoading.set(false);
                    this.error.set('DIETOLOGIST.CLIENT_DASHBOARD.LOAD_ERROR');
                },
            });
    }

    protected goBack(): void {
        void this.router.navigate(['/dietologist']);
    }

    protected startClientDashboardTour(force = true): void {
        this.tourService.start(this.localizedTour.build(CLIENT_DASHBOARD_TOUR), { force });
    }

    protected applyDateFilter(): void {
        void this.applyDateFilterAsync();
    }

    private async applyDateFilterAsync(): Promise<void> {
        const client = this.client();
        const nextPeriod = this.getPeriodFromForm();
        if (
            client === null ||
            nextPeriod === null ||
            this.dateFilterForm().invalid() ||
            (nextPeriod.dateFrom === this.selectedDateFrom() && nextPeriod.dateTo === this.selectedDateTo())
        ) {
            this.dateFilterForm().markAsTouched();
            return;
        }

        this.selectedDateFrom.set(nextPeriod.dateFrom);
        this.selectedDateTo.set(nextPeriod.dateTo);
        await this.reloadDashboardAsync(client);
    }

    protected showPreviousPeriod(): void {
        this.shiftSelectedPeriod(-this.getSelectedPeriodLength());
    }

    protected showNextPeriod(): void {
        this.shiftSelectedPeriod(this.getSelectedPeriodLength());
    }

    protected showRecentDays(days: number): void {
        const dateTo = new Date();
        const dateFrom = this.addDays(dateTo, -(days - 1));
        this.applyPeriod(formatDateInputValue(dateFrom), formatDateInputValue(dateTo));
    }

    protected showToday(): void {
        const today = formatDateInputValue(new Date());
        this.applyPeriod(today, today);
    }

    protected submitRecommendation(): void {
        void this.submitRecommendationAsync();
    }

    protected useRecommendationTemplate(template: RecommendationTemplate): void {
        this.recommendationModel.update(model => ({ ...model, text: template.text }));
    }

    protected createRecommendationTemplate(): void {
        const model = this.recommendationModel();
        const name = model.templateName.trim();
        const text = model.text.trim();
        if (name.length === 0 || text.length === 0 || this.savingTemplate()) {
            return;
        }

        this.savingTemplate.set(true);
        this.dietologistFacade
            .createRecommendationTemplate({ name, text })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: template => {
                    this.recommendationTemplates.update(templates =>
                        [...templates, template].sort((left, right) => left.name.localeCompare(right.name)),
                    );
                    this.recommendationModel.update(value => ({ ...value, templateName: '' }));
                    this.savingTemplate.set(false);
                    this.toastService.success(this.translateService.instant('RECOMMENDATION_TEMPLATES.CREATED'));
                },
                error: () => {
                    this.savingTemplate.set(false);
                    this.toastService.error(this.translateService.instant('RECOMMENDATION_TEMPLATES.SAVE_ERROR'));
                },
            });
    }

    protected archiveRecommendationTemplate(template: RecommendationTemplate): void {
        this.dietologistFacade
            .archiveRecommendationTemplate(template.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.recommendationTemplates.update(templates => templates.filter(item => item.id !== template.id));
                },
                error: () => {
                    this.toastService.error(this.translateService.instant('RECOMMENDATION_TEMPLATES.ARCHIVE_ERROR'));
                },
            });
    }

    protected cancelTask(task: ClientTask): void {
        if (this.changingTaskIds().has(task.id)) {
            return;
        }

        this.changingTaskIds.update(ids => new Set(ids).add(task.id));
        this.dietologistFacade
            .cancelTask(task.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: updated => {
                    this.tasks.update(tasks => tasks.map(item => (item.id === updated.id ? updated : item)));
                    this.removeChangingTaskId(task.id);
                },
                error: () => {
                    this.toastService.error(this.translateService.instant('CLIENT_TASKS.CHANGE_ERROR'));
                    this.removeChangingTaskId(task.id);
                },
            });
    }

    private async submitRecommendationAsync(): Promise<void> {
        const client = this.client();
        if (client === null || this.recommendationForm().invalid() || this.savingRecommendation()) {
            this.recommendationForm().markAsTouched();
            return;
        }

        const text = this.recommendationModel().text.trim();
        if (text.length === 0) {
            this.recommendationModel.update(model => ({ ...model, text: '' }));
            this.recommendationForm().markAsTouched();
            return;
        }

        this.savingRecommendation.set(true);
        try {
            const recommendation = await firstValueFrom(
                this.dietologistFacade.createRecommendation(client.userId, { text }).pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.recommendations.update(items => [recommendation, ...items]);
            this.recommendationModel.update(model => ({ ...model, text: '' }));
            this.savingRecommendation.set(false);
            this.toastService.success(this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.RECOMMENDATIONS.SENT'));
        } catch {
            this.savingRecommendation.set(false);
            this.toastService.error(this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.RECOMMENDATIONS.SEND_ERROR'));
        }
    }

    private async submitTaskAsync(): Promise<void> {
        const client = this.client();
        if (client === null || this.taskForm().invalid() || this.savingTask()) {
            this.taskForm().markAsTouched();
            return;
        }

        const value = this.taskModel();
        const dueDate = parseLocalDateInputValue(value.dueDate);
        this.savingTask.set(true);
        try {
            const task = await firstValueFrom(
                this.dietologistFacade
                    .createTask(client.userId, {
                        title: value.title.trim(),
                        details: value.details.trim() === '' ? null : value.details.trim(),
                        dueAtUtc: dueDate?.toISOString() ?? null,
                    })
                    .pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.tasks.update(tasks => [task, ...tasks]);
            this.taskModel.set({ title: '', details: '', dueDate: '' });
            this.toastService.success(this.translateService.instant('CLIENT_TASKS.CREATED'));
        } catch {
            this.toastService.error(this.translateService.instant('CLIENT_TASKS.CREATE_ERROR'));
        } finally {
            this.savingTask.set(false);
        }
    }

    protected disconnectClient(): void {
        const client = this.client();
        if (client === null) {
            return;
        }

        this.dialogService
            .open(FdUiConfirmDialogComponent, {
                preset: 'confirm',
                data: {
                    title: this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECT_TITLE'),
                    message: this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECT_MESSAGE'),
                    confirmLabel: this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECT_CONFIRM'),
                    cancelLabel: this.translateService.instant('COMMON.CANCEL'),
                    danger: true,
                },
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(confirmed => {
                if (confirmed === true) {
                    this.executeDisconnectClient(client);
                }
            });
    }

    private executeDisconnectClient(client: ClientSummary): void {
        this.dietologistFacade
            .disconnectClient(client.userId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.toastService.info(this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECTED'));
                    void this.router.navigate(['/dietologist']);
                },
                error: () => {
                    this.toastService.error(this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECT_ERROR'));
                },
            });
    }

    private localizeProfileValue(value: string, client: ClientSummary | null): string {
        if (value === client?.activityLevel) {
            return this.translateService.instant(`USER_MANAGE.ACTIVITY_LEVEL_OPTIONS.${value.toUpperCase()}`);
        }

        if (value === client?.gender) {
            const genderKey = value.toUpperCase() === 'MALE' ? 'M' : value.toUpperCase() === 'FEMALE' ? 'F' : value.toUpperCase();
            return this.translateService.instant(`USER_MANAGE.GENDER_OPTIONS.${genderKey}`);
        }

        return value;
    }

    private loadClientDetails(client: ClientSummary): Observable<{
        dashboard: DashboardSnapshot | null;
        goals: DietologistClientGoals | null;
        recommendations: DietologistRecommendation[];
        tasks: ClientTask[];
        templates: RecommendationTemplate[];
    }> {
        const language = resolveTranslateLanguage(this.translateService);
        this.sectionLoadError.set(null);

        return forkJoin({
            dashboard: this.shouldLoadDashboardSnapshot(client)
                ? this.loadDashboardSnapshot(client, language).pipe(this.handleSectionLoadError<DashboardSnapshot, null>(null))
                : of(null),
            goals: client.permissions.shareGoals
                ? this.dietologistFacade.getClientGoals(client.userId).pipe(this.handleSectionLoadError<DietologistClientGoals, null>(null))
                : of(null),
            recommendations: this.dietologistFacade
                .getRecommendationsForClient(client.userId)
                .pipe(this.handleSectionLoadError<DietologistRecommendation[], DietologistRecommendation[]>([])),
            tasks: this.dietologistFacade
                .getTasksForClient(client.userId)
                .pipe(this.handleSectionLoadError<ClientTask[], ClientTask[]>([])),
            templates: this.dietologistFacade
                .searchRecommendationTemplates()
                .pipe(this.handleSectionLoadError<RecommendationTemplate[], RecommendationTemplate[]>([])),
        }).pipe(
            tap(result => {
                this.dashboard.set(result.dashboard);
                this.goals.set(result.goals);
                this.recommendations.set(result.recommendations);
                this.tasks.set(result.tasks);
                this.recommendationTemplates.set(result.templates);
            }),
        );
    }

    private reloadDashboard(client: ClientSummary): void {
        void this.reloadDashboardAsync(client);
    }

    private async reloadDashboardAsync(client: ClientSummary): Promise<void> {
        if (!this.shouldLoadDashboardSnapshot(client)) {
            this.dashboard.set(null);
            return;
        }

        this.detailsLoading.set(true);
        this.sectionLoadError.set(null);
        const result = await firstValueFrom(
            this.loadDashboardSnapshot(client, resolveTranslateLanguage(this.translateService))
                .pipe(this.handleSectionLoadError<DashboardSnapshot, null>(null))
                .pipe(takeUntilDestroyed(this.destroyRef)),
        );
        this.dashboard.set(result);
        this.detailsLoading.set(false);
    }

    private loadDashboardSnapshot(client: ClientSummary, language: string): Observable<DashboardSnapshot> {
        const period = this.getSelectedPeriodValue();
        return this.dietologistFacade.getClientDashboard(client.userId, {
            dateFrom: period.dateFrom,
            dateTo: period.dateTo,
            locale: language,
            trendDays: CLIENT_DASHBOARD_TREND_DAYS,
        });
    }

    private shouldLoadDashboardSnapshot(client: ClientSummary): boolean {
        return (
            client.permissions.shareStatistics ||
            client.permissions.shareMeals ||
            client.permissions.shareWeight ||
            client.permissions.shareWaist ||
            client.permissions.shareHydration ||
            client.permissions.shareFasting
        );
    }

    private shiftSelectedPeriod(days: number): void {
        const period = this.getSelectedPeriodValue();
        const nextFrom = formatDateInputValue(this.addDays(period.dateFrom, days));
        const nextTo = formatDateInputValue(this.addDays(period.dateTo, days));
        this.applyPeriod(nextFrom, nextTo);
    }

    private applyPeriod(dateFrom: string, dateTo: string): void {
        this.dateFilterModel.set({ dateFrom, dateTo });
        const client = this.client();
        if (client === null) {
            this.selectedDateFrom.set(dateFrom);
            this.selectedDateTo.set(dateTo);
            return;
        }

        this.selectedDateFrom.set(dateFrom);
        this.selectedDateTo.set(dateTo);
        this.reloadDashboard(client);
    }

    private getSelectedPeriodValue(): { dateFrom: Date; dateTo: Date } {
        const parsed = this.getPeriodFromForm();
        return {
            dateFrom:
                parseLocalDateInputValue(parsed?.dateFrom ?? this.selectedDateFrom()) ??
                this.addDays(new Date(), -(DEFAULT_PERIOD_DAYS - 1)),
            dateTo: parseLocalDateInputValue(parsed?.dateTo ?? this.selectedDateTo()) ?? new Date(),
        };
    }

    private getPeriodFromForm(): { dateFrom: string; dateTo: string } | null {
        const { dateFrom, dateTo } = this.dateFilterModel();
        const parsedFrom = parseLocalDateInputValue(dateFrom);
        const parsedTo = parseLocalDateInputValue(dateTo);
        if (parsedFrom === null || parsedTo === null || parsedFrom > parsedTo) {
            return null;
        }

        return { dateFrom, dateTo };
    }

    private getSelectedPeriodLength(): number {
        const period = this.getSelectedPeriodValue();
        return Math.max(1, Math.round((period.dateTo.getTime() - period.dateFrom.getTime()) / MILLISECONDS_PER_DAY) + 1);
    }

    private addDays(date: Date, days: number): Date {
        const next = new Date(date);
        next.setDate(next.getDate() + days);
        return next;
    }

    private handleSectionLoadError<T, TFallback>(fallback: TFallback): (source: Observable<T>) => Observable<T | TFallback> {
        return source =>
            source.pipe(
                catchError(() => {
                    this.sectionLoadError.set('DIETOLOGIST.CLIENT_DASHBOARD.PARTIAL_LOAD_ERROR');
                    return of(fallback);
                }),
            );
    }

    private removeChangingTaskId(taskId: string): void {
        this.changingTaskIds.update(ids => {
            const next = new Set(ids);
            next.delete(taskId);
            return next;
        });
    }
}
