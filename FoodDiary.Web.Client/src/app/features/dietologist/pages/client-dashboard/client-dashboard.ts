import { UpperCasePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, FormField, maxLength, required, validate } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiFormDirective, focusFirstInvalidField } from 'fd-ui-kit/form/fd-ui-form.directive';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import {
    catchError,
    distinctUntilChanged,
    finalize,
    firstValueFrom,
    forkJoin,
    map,
    type Observable,
    of,
    Subject,
    switchMap,
    takeUntil,
    tap,
} from 'rxjs';

import { devDay, devEnum, devRecord, devText } from '../../../../shared/development/development-input';
import { connectDevelopmentScreen, DevelopmentToolError } from '../../../../shared/development/development-tools';
import { LocalizedDatePipe } from '../../../../shared/i18n/localized-date.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateInputValue, normalizeEndOfLocalDay, parseLocalDateInputValue } from '../../../../shared/lib/local-date.utils';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import type {
    ClientSummary,
    ClientTask,
    DietologistClientGoals,
    DietologistRecommendation,
    RecommendationTemplate,
} from '../../../../shared/models/dietologist.data';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { RecommendationThreadComponent } from '../../../recommendations/contracts/recommendation-thread';
import { createClientValueFormatting } from '../../lib/client-value-formatting';
import { DietologistFacade } from '../../lib/dietologist.facade';
import type { DietologistDashboardSnapshot } from '../../lib/dietologist-dashboard.data';
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
import { appendClientMealPage } from './client-dashboard-lib/client-meal-pages';
import { CLIENT_DASHBOARD_TOUR } from './client-dashboard-tour';
import { ClientDashboardFastingCardComponent } from './components/client-dashboard-fasting-card';
import { ClientDashboardHeaderComponent } from './components/client-dashboard-header';
import { ClientDashboardHydrationCardComponent } from './components/client-dashboard-hydration-card';
import { ClientDashboardMealsCardComponent } from './components/client-dashboard-meals-card';
import { ClientDashboardMetricListComponent } from './components/client-dashboard-metric-list';
import { ClientDashboardNoticesComponent } from './components/client-dashboard-notices';
import { ClientDashboardSummaryCardComponent } from './components/client-dashboard-summary-card';

const RECOMMENDATION_MAX_LENGTH = 2000;
const TEMPLATE_NAME_MAX_LENGTH = 120;
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

type ClientDashboardDetails = {
    dashboard: DietologistDashboardSnapshot | null;
    goals: DietologistClientGoals | null;
    recommendations: DietologistRecommendation[];
    tasks: ClientTask[];
    templates: RecommendationTemplate[];
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
        UpperCasePipe,
        LocalizedDatePipe,
        FormField,
        FdUiFormDirective,
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
    private readonly templateNameInput = viewChild<FdUiInputComponent>('templateNameInput');
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly dietologistFacade = inject(DietologistFacade);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly measurements = inject(MeasurementSystemService);
    private readonly clientChanged = new Subject<void>();
    private clientRevision = 0;
    private dashboardReloadSequence = 0;
    private loadedMealPage = 1;

    protected readonly periodPresetDays = PERIOD_PRESET_DAYS;
    protected readonly client = signal<ClientSummary | null>(null);
    protected readonly dashboard = signal<DietologistDashboardSnapshot | null>(null);
    protected readonly goals = signal<DietologistClientGoals | null>(null);
    protected readonly recommendations = signal<DietologistRecommendation[]>([]);
    protected readonly recommendationTemplates = signal<RecommendationTemplate[]>([]);
    protected readonly tasks = signal<ClientTask[]>([]);
    protected readonly loading = signal(true);
    protected readonly detailsLoading = signal(false);
    protected readonly loadingMoreMeals = signal(false);
    protected readonly moreMealsError = signal(false);
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
            validate(path.dateTo, context => {
                const start = parseLocalDateInputValue(context.valueOf(path.dateFrom));
                const end = parseLocalDateInputValue(context.value());
                return start !== null && end !== null && start > end ? { kind: 'dateOrder' } : undefined;
            });
        },
        {
            submission: {
                action: this.submitDateFilterFormAsync,
            },
        },
    );
    protected readonly dateFilterRangeInvalid = computed(() =>
        this.dateFilterForm
            .dateTo()
            .errors()
            .some(error => error.kind === 'dateOrder'),
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
            disabled(path.text, { when: () => this.savingRecommendation() || this.savingTemplate() });
            disabled(path.templateName, { when: () => this.savingTemplate() });
        },
        {
            submission: {
                action: this.submitRecommendationFormAsync,
            },
        },
    );
    protected readonly taskModel = signal<TaskFormModel>({ title: '', details: '', dueDate: '' });
    protected readonly templateNameTooLong = computed(
        () => this.recommendationModel().templateName.trim().length > TEMPLATE_NAME_MAX_LENGTH,
    );
    protected readonly templateAttempted = signal(false);
    protected readonly templateNameError = computed(() => {
        this.translateService.currentLang();
        if (this.templateNameTooLong()) {
            return this.translateService.instant('RECOMMENDATION_TEMPLATES.NAME_TOO_LONG');
        }
        if (this.templateAttempted() && this.recommendationModel().templateName.trim().length === 0) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }
        return null;
    });
    protected readonly templateInvalid = computed(
        () =>
            this.savingRecommendation() ||
            this.savingTemplate() ||
            this.recommendationModel().templateName.trim().length === 0 ||
            this.recommendationModel().text.trim().length === 0 ||
            this.templateNameTooLong() ||
            this.recommendationForm().invalid(),
    );
    private readonly submitTaskFormAsync = async (): Promise<void> => {
        await this.submitTaskAsync();
    };
    protected readonly taskForm = form(
        this.taskModel,
        path => {
            required(path.title);
            validate(path.title, context => {
                const title = context.value();
                return title.length > 0 && title.trim().length === 0 ? { kind: 'required' } : undefined;
            });
            maxLength(path.title, TASK_TITLE_MAX_LENGTH);
            maxLength(path.details, TASK_DETAILS_MAX_LENGTH);
            disabled(path.title, { when: () => this.savingTask() });
            disabled(path.details, { when: () => this.savingTask() });
            disabled(path.dueDate, { when: () => this.savingTask() });
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
            return this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.TITLE');
        }

        return getClientDashboardTitle(client) ?? this.translateService.instant('ATTENTION.UNNAMED_CLIENT');
    });
    private readonly valueFormatting = computed(() =>
        createClientValueFormatting(this.translateService.currentLang() ?? 'en', key => this.translateService.instant(key)),
    );
    protected readonly canRecommend = computed(() => {
        const client = this.client();
        return client !== null && Object.values(client.permissions).some(Boolean);
    });
    protected readonly recommendationDisabled = computed(
        () => !this.canRecommend() || this.savingRecommendation() || this.savingTemplate(),
    );
    protected readonly recommendationDisabledReason = computed(() => {
        this.translateService.currentLang();
        if (!this.canRecommend()) {
            return this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.RECOMMENDATIONS.PERMISSIONS_REQUIRED');
        }

        return null;
    });
    protected readonly profileChips = computed(() => {
        const client = this.client();
        return buildClientProfileChips(client, this.measurements.system(), this.valueFormatting()).map(value =>
            this.localizeProfileValue(value, client),
        );
    });
    protected readonly profileDetails = computed<ClientProfileDetail[]>(() => {
        const client = this.client();
        return buildClientProfileDetails(client, this.measurements.system(), this.valueFormatting()).map(detail => ({
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
        return client !== null && this.shouldLoadDietologistDashboardSnapshot(client);
    });
    protected readonly nutritionTiles = computed<ClientMetricTile[]>(() =>
        this.client()?.permissions.shareStatistics === true ? buildNutritionTiles(this.dashboard(), this.valueFormatting()) : [],
    );
    protected readonly bodyTiles = computed<ClientMetricTile[]>(() =>
        buildBodyTiles(this.dashboard(), this.client()?.permissions, this.measurements.system(), this.valueFormatting()),
    );
    protected readonly goalTiles = computed<ClientMetricTile[]>(() =>
        buildGoalTiles(this.goals(), this.measurements.system(), this.valueFormatting()),
    );
    protected readonly mealItems = computed<ClientMealView[]>(() =>
        this.client()?.permissions.shareMeals === true
            ? buildMealViews(
                  this.dashboard(),
                  this.valueFormatting(),
                  key => this.translateService.instant(key),
                  this.translateService.currentLang() ?? 'en',
              )
            : [],
    );
    protected readonly weightSummary = computed<ClientBodyMeasurementView | null>(() =>
        this.client()?.permissions.shareWeight === true
            ? buildWeightView(this.dashboard(), this.measurements.system(), this.valueFormatting())
            : null,
    );
    protected readonly waistSummary = computed<ClientBodyMeasurementView | null>(() =>
        this.client()?.permissions.shareWaist === true
            ? buildWaistView(this.dashboard(), this.measurements.system(), this.valueFormatting())
            : null,
    );
    protected readonly hydrationSummary = computed<ClientHydrationView | null>(() =>
        this.client()?.permissions.shareHydration === true ? buildHydrationView(this.dashboard(), this.valueFormatting()) : null,
    );
    protected readonly fastingSummary = computed<ClientFastingView | null>(() =>
        this.client()?.permissions.shareFasting === true
            ? buildFastingView(this.dashboard(), this.valueFormatting(), key => this.translateService.instant(key))
            : null,
    );
    protected readonly recommendationItems = computed<ClientRecommendationView[]>(() => buildRecommendationViews(this.recommendations()));

    public constructor() {
        this.connectDevelopmentTools();
        this.route.queryParamMap
            .pipe(
                map(params => params.get('recommendationId')),
                distinctUntilChanged(),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(id => {
                this.openDiscussionId.set(id);
            });
        this.route.paramMap
            .pipe(
                map(params => params.get('clientId')),
                distinctUntilChanged(),
                switchMap(clientId => {
                    this.resetClientState();
                    return this.loadRouteClient(clientId);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();
    }

    private loadRouteClient(clientId: string | null): Observable<ClientDashboardDetails | null> {
        return this.dietologistFacade.getMyClients().pipe(
            switchMap(clients => {
                const found = clients.find(client => client.userId === clientId) ?? null;
                this.client.set(found);
                this.loading.set(false);
                if (found === null) {
                    return of(null);
                }
                this.detailsLoading.set(true);
                return this.loadClientDetails(found);
            }),
            catchError(() => {
                this.loading.set(false);
                this.detailsLoading.set(false);
                this.error.set('DIETOLOGIST.CLIENT_DASHBOARD.LOAD_ERROR');
                return of(null);
            }),
        );
    }

    private resetClientState(): void {
        this.clientRevision++;
        this.dashboardReloadSequence++;
        this.clientChanged.next();
        this.client.set(null);
        this.dashboard.set(null);
        this.goals.set(null);
        this.recommendations.set([]);
        this.tasks.set([]);
        this.recommendationTemplates.set([]);
        this.loading.set(true);
        this.detailsLoading.set(false);
        this.loadingMoreMeals.set(false);
        this.moreMealsError.set(false);
        this.savingRecommendation.set(false);
        this.savingTask.set(false);
        this.savingTemplate.set(false);
        this.changingTaskIds.set(new Set());
        this.loadedMealPage = 1;
        this.error.set(null);
        this.sectionLoadError.set(null);
        this.openDiscussionId.set(this.route.snapshot.queryParamMap.get('recommendationId'));
        this.recommendationForm().reset({ text: '', templateName: '' });
        this.taskForm().reset({ title: '', details: '', dueDate: '' });
    }

    private isCurrentClient(revision: number): boolean {
        return !this.destroyRef.destroyed && revision === this.clientRevision;
    }

    private isCurrentClientResult<T>(value: T, revision: number): value is NonNullable<T> {
        return value !== null && value !== undefined && this.isCurrentClient(revision);
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

    protected loadMoreMeals(): void {
        void this.loadMoreMealsAsync();
    }

    private async loadMoreMealsAsync(): Promise<void> {
        const client = this.client();
        const snapshot = this.dashboard();
        if (client === null || snapshot === null || !this.canLoadMoreMeals(client, snapshot)) {
            return;
        }

        const sequence = this.dashboardReloadSequence;
        const nextPage = this.loadedMealPage + 1;
        this.loadingMoreMeals.set(true);
        this.moreMealsError.set(false);
        try {
            const result = await firstValueFrom(
                this.loadDietologistDashboardSnapshot(client, resolveTranslateLanguage(this.translateService), nextPage).pipe(
                    catchError(() => of(null)),
                    takeUntil(this.clientChanged),
                    takeUntilDestroyed(this.destroyRef),
                ),
                { defaultValue: null },
            );
            if (this.destroyRef.destroyed || sequence !== this.dashboardReloadSequence) {
                return;
            }
            if (result === null) {
                this.moreMealsError.set(true);
                return;
            }

            this.dashboard.set(appendClientMealPage(snapshot, result));
            this.loadedMealPage = nextPage;
        } finally {
            if (!this.destroyRef.destroyed && sequence === this.dashboardReloadSequence) {
                this.loadingMoreMeals.set(false);
            }
        }
    }

    private canLoadMoreMeals(client: ClientSummary, snapshot: DietologistDashboardSnapshot): boolean {
        return (
            client.permissions.shareMeals &&
            snapshot.meals.items.length < snapshot.meals.total &&
            !this.detailsLoading() &&
            !this.loadingMoreMeals()
        );
    }

    protected submitRecommendation(): void {
        void this.submitRecommendationAsync();
    }

    protected useRecommendationTemplate(template: RecommendationTemplate): void {
        if (this.savingRecommendation() || this.savingTemplate()) {
            return;
        }
        this.recommendationModel.update(model => ({ ...model, text: template.text }));
    }

    private focusTemplateError(): void {
        if (this.recommendationModel().templateName.trim().length === 0 || this.templateNameTooLong()) {
            this.templateNameInput()?.focus();
        } else {
            focusFirstInvalidField(this.recommendationForm);
        }
    }

    protected createRecommendationTemplate(): void {
        if (this.savingTemplate() || this.savingRecommendation() || !this.canRecommend()) {
            return;
        }
        this.templateAttempted.set(true);
        this.recommendationForm().markAsTouched();
        const model = this.recommendationModel();
        const name = model.templateName.trim();
        const text = model.text.trim();
        if (this.templateInvalid()) {
            this.focusTemplateError();
            return;
        }

        this.savingTemplate.set(true);
        this.dietologistFacade
            .createRecommendationTemplate({ name, text })
            .pipe(takeUntil(this.clientChanged), takeUntilDestroyed(this.destroyRef))
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
            .pipe(takeUntil(this.clientChanged), takeUntilDestroyed(this.destroyRef))
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
            .pipe(takeUntil(this.clientChanged), takeUntilDestroyed(this.destroyRef))
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
        if (client === null || this.recommendationDisabled() || this.recommendationForm().invalid()) {
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
        const revision = this.clientRevision;
        try {
            const recommendation = await firstValueFrom(
                this.dietologistFacade
                    .createRecommendation(client.userId, { text })
                    .pipe(takeUntil(this.clientChanged), takeUntilDestroyed(this.destroyRef)),
                { defaultValue: null },
            );
            if (!this.isCurrentClientResult(recommendation, revision)) {
                return;
            }
            this.recommendations.update(items => [recommendation, ...items]);
            this.recommendationModel.update(model => ({ ...model, text: '' }));
            this.savingRecommendation.set(false);
            this.toastService.success(this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.RECOMMENDATIONS.SENT'));
        } catch {
            if (!this.isCurrentClient(revision)) {
                return;
            }
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
        const revision = this.clientRevision;
        try {
            const task = await firstValueFrom(
                this.dietologistFacade
                    .createTask(client.userId, {
                        title: value.title.trim(),
                        details: value.details.trim() === '' ? null : value.details.trim(),
                        dueAtUtc: dueDate === null ? null : normalizeEndOfLocalDay(dueDate).toISOString(),
                    })
                    .pipe(takeUntil(this.clientChanged), takeUntilDestroyed(this.destroyRef)),
                { defaultValue: null },
            );
            if (!this.isCurrentClientResult(task, revision)) {
                return;
            }
            this.tasks.update(tasks => [task, ...tasks]);
            this.taskModel.set({ title: '', details: '', dueDate: '' });
            this.toastService.success(this.translateService.instant('CLIENT_TASKS.CREATED'));
        } catch {
            if (this.isCurrentClient(revision)) {
                this.toastService.error(this.translateService.instant('CLIENT_TASKS.CREATE_ERROR'));
            }
        } finally {
            if (this.isCurrentClient(revision)) {
                this.savingTask.set(false);
            }
        }
    }

    protected disconnectClient(): void {
        const client = this.client();
        if (client === null) {
            return;
        }

        const dialogRef = this.dialogService.open(FdUiConfirmDialogComponent, {
            preset: 'confirm',
            data: {
                title: this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECT_TITLE'),
                message: this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECT_MESSAGE'),
                confirmLabel: this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECT_CONFIRM'),
                cancelLabel: this.translateService.instant('COMMON.CANCEL'),
                danger: true,
            },
        });
        const unregisterCleanup = this.destroyRef.onDestroy(() => {
            dialogRef.close();
        });
        const clientChange = this.clientChanged.subscribe(() => {
            dialogRef.close();
        });
        dialogRef
            .afterClosed()
            .pipe(
                takeUntil(this.clientChanged),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    clientChange.unsubscribe();
                    unregisterCleanup();
                }),
            )
            .subscribe(confirmed => {
                if (confirmed === true) {
                    this.executeDisconnectClient(client);
                }
            });
    }

    private executeDisconnectClient(client: ClientSummary): void {
        this.dietologistFacade
            .disconnectClient(client.userId)
            .pipe(takeUntil(this.clientChanged), takeUntilDestroyed(this.destroyRef))
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

    private loadClientDetails(client: ClientSummary): Observable<ClientDashboardDetails> {
        const reloadSequence = ++this.dashboardReloadSequence;
        const revision = this.clientRevision;
        const language = resolveTranslateLanguage(this.translateService);
        this.sectionLoadError.set(null);

        return forkJoin({
            dashboard: this.shouldLoadDietologistDashboardSnapshot(client)
                ? this.loadDietologistDashboardSnapshot(client, language).pipe(
                      this.handleSectionLoadError<DietologistDashboardSnapshot, null>(null, reloadSequence),
                  )
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
                if (!this.isCurrentClient(revision)) {
                    return;
                }
                this.applyDashboardIfCurrent(result.dashboard, reloadSequence);
                this.goals.set(result.goals);
                this.recommendations.update(items => this.mergeInitialItems(result.recommendations, items));
                this.tasks.update(items => this.mergeInitialItems(result.tasks, items));
                this.recommendationTemplates.update(items =>
                    this.mergeInitialItems(result.templates, items).sort((left, right) => left.name.localeCompare(right.name)),
                );
            }),
        );
    }

    private reloadDashboard(client: ClientSummary): void {
        void this.reloadDashboardAsync(client);
    }

    private async reloadDashboardAsync(client: ClientSummary): Promise<void> {
        const reloadSequence = ++this.dashboardReloadSequence;
        this.loadingMoreMeals.set(false);
        this.moreMealsError.set(false);
        if (!this.shouldLoadDietologistDashboardSnapshot(client)) {
            this.applyDashboardIfCurrent(null, reloadSequence);
            return;
        }

        this.detailsLoading.set(true);
        this.sectionLoadError.set(null);
        const result = await firstValueFrom(
            this.loadDietologistDashboardSnapshot(client, resolveTranslateLanguage(this.translateService))
                .pipe(this.handleSectionLoadError<DietologistDashboardSnapshot, null>(null, reloadSequence))
                .pipe(takeUntil(this.clientChanged))
                .pipe(takeUntilDestroyed(this.destroyRef)),
            { defaultValue: null },
        );

        if (this.destroyRef.destroyed) {
            return;
        }

        this.applyDashboardIfCurrent(result, reloadSequence);
    }

    private applyDashboardIfCurrent(dashboard: DietologistDashboardSnapshot | null, reloadSequence: number): void {
        if (reloadSequence === this.dashboardReloadSequence) {
            this.dashboard.set(dashboard);
            this.loadedMealPage = 1;
            this.detailsLoading.set(false);
        }
    }

    private mergeInitialItems<T extends { id: string }>(loaded: T[], current: T[]): T[] {
        const currentIds = new Set(current.map(item => item.id));
        return [...current, ...loaded.filter(item => !currentIds.has(item.id))];
    }

    private loadDietologistDashboardSnapshot(client: ClientSummary, language: string, page = 1): Observable<DietologistDashboardSnapshot> {
        const period = this.getSelectedPeriodValue();
        return this.dietologistFacade.getClientDashboard(client.userId, {
            dateFrom: period.dateFrom,
            dateTo: period.dateTo,
            page,
            locale: language,
            trendDays: CLIENT_DASHBOARD_TREND_DAYS,
        });
    }

    private shouldLoadDietologistDashboardSnapshot(client: ClientSummary): boolean {
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

    private handleSectionLoadError<T, TFallback>(
        fallback: TFallback,
        reloadSequence?: number,
    ): (source: Observable<T>) => Observable<T | TFallback> {
        return source =>
            source.pipe(
                catchError(() => {
                    if (reloadSequence === undefined || reloadSequence === this.dashboardReloadSequence) {
                        this.sectionLoadError.set('DIETOLOGIST.CLIENT_DASHBOARD.PARTIAL_LOAD_ERROR');
                    }
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

    private connectDevelopmentTools(): void {
        connectDevelopmentScreen({
            kind: 'client-dashboard',
            read: () => this.readDevelopmentContext(),
            isDirty: () => this.hasDevelopmentDraft(),
            draftSchema: {
                type: 'object',
                description:
                    'Recommendation: {type:"recommendation",text:string}. Task: {type:"task",title:string,details:string,dueDate:"YYYY-MM-DD"|""}. Never sends.',
                properties: {
                    type: { enum: ['recommendation', 'task'] },
                    text: { type: 'string', maxLength: RECOMMENDATION_MAX_LENGTH },
                    title: { type: 'string', maxLength: TASK_TITLE_MAX_LENGTH },
                    details: { type: 'string', maxLength: TASK_DETAILS_MAX_LENGTH },
                    dueDate: { type: 'string' },
                },
            },
            fillDraft: input => {
                this.fillDevelopmentDraft(input);
            },
            clearDraft: () => {
                if (this.savingRecommendation() || this.savingTask()) {
                    throw new DevelopmentToolError('A save is running.');
                }
                this.recommendationForm().reset({ text: '', templateName: '' });
                this.taskForm().reset({ title: '', details: '', dueDate: '' });
            },
            setFilters: input => {
                const filters = devRecord(input, ['dateFrom', 'dateTo', 'timeZone']);
                const dateFrom = devDay(filters['dateFrom']);
                const dateTo = devDay(filters['dateTo']);
                if (filters['timeZone'] !== new Intl.DateTimeFormat().resolvedOptions().timeZone || dateFrom > dateTo) {
                    throw new DevelopmentToolError('Use the current browser timezone and an ordered calendar range.');
                }
                this.applyPeriod(dateFrom, dateTo);
                return;
            },
        });
    }

    private fillDevelopmentDraft(input: unknown): void {
        if (this.loading() || this.detailsLoading() || this.client() === null || this.savingRecommendation() || this.savingTask()) {
            throw new DevelopmentToolError('Client is not ready.');
        }
        const draft = devRecord(input, ['type', 'text', 'title', 'details', 'dueDate']);
        const type = devEnum(draft['type'], ['recommendation', 'task']);
        if (type === 'recommendation') {
            if (!this.canRecommend()) {
                throw new DevelopmentToolError('Client permissions do not allow recommendations.');
            }
            this.recommendationModel.set({ text: devText(draft['text'], RECOMMENDATION_MAX_LENGTH, true), templateName: '' });
            this.recommendationForm().markAsDirty();
        } else {
            const dueDate = draft['dueDate'] === '' ? '' : devDay(draft['dueDate']);
            this.taskModel.set({
                title: devText(draft['title'], TASK_TITLE_MAX_LENGTH, true),
                details: devText(draft['details'], TASK_DETAILS_MAX_LENGTH, true),
                dueDate,
            });
            this.taskForm().markAsDirty();
        }
    }

    private hasDevelopmentDraft(): boolean {
        return (
            this.recommendationModel().text.length > 0 ||
            this.recommendationModel().templateName.length > 0 ||
            this.taskModel().title.length > 0 ||
            this.taskModel().details.length > 0 ||
            this.taskModel().dueDate.length > 0
        );
    }

    private readDevelopmentContext(): unknown {
        return {
            clientId: this.client()?.userId ?? null,
            permissions: this.client()?.permissions ?? null,
            filterFields: ['dateFrom', 'dateTo', 'timeZone'],
            dateFrom: this.selectedDateFrom(),
            dateTo: this.selectedDateTo(),
            loading: this.loading() || this.detailsLoading(),
            error: this.error() ?? this.sectionLoadError(),
            meals: this.readDevelopmentMeals(),
            recommendation: {
                ...this.recommendationModel(),
                valid: this.recommendationForm().valid(),
                disabledReason: this.recommendationDisabledReason(),
            },
            task: { ...this.taskModel(), valid: this.taskForm().valid() },
        };
    }

    private readDevelopmentMeals(): unknown {
        const shared = this.client()?.permissions.shareMeals === true;
        const loading = this.loading() || this.detailsLoading();
        const meals = shared && !loading ? this.dashboard()?.meals : undefined;
        if (meals === undefined) {
            return {
                available: false,
                permissionGranted: shared,
                total: null,
                loadedCount: 0,
                loadedPage: null,
                hasNext: false,
                loading,
                loadingMore: this.loadingMoreMeals(),
                loadMoreError: this.moreMealsError(),
                items: [],
            };
        }
        return {
            available: true,
            permissionGranted: shared,
            total: meals.total,
            loadedCount: meals.items.length,
            loadedPage: this.loadedMealPage,
            hasNext: meals.items.length < meals.total,
            loading,
            loadingMore: this.loadingMoreMeals(),
            loadMoreError: this.moreMealsError(),
            items: meals.items.map(meal => ({ id: meal.id, dateUtc: meal.date })),
        };
    }
}
