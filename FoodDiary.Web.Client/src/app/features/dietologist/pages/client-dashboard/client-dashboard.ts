import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { catchError, forkJoin, type Observable, of, switchMap, tap } from 'rxjs';

import { formatDateInputValue, parseLocalDateInputValue } from '../../../../shared/lib/local-date.utils';
import type { ClientSummary, DietologistClientGoals, DietologistRecommendation } from '../../../../shared/models/dietologist.data';
import type { DashboardSnapshot } from '../../../dashboard/models/dashboard.data';
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

const RECOMMENDATION_MAX_LENGTH = 2000;
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

@Component({
    selector: 'fd-client-dashboard',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        DatePipe,
        ReactiveFormsModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiDateInputComponent,
        FdUiTextareaComponent,
    ],
    templateUrl: './client-dashboard.html',
    styleUrls: ['./client-dashboard.scss'],
})
export class ClientDashboardComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly dietologistFacade = inject(DietologistFacade);
    private readonly formBuilder = inject(NonNullableFormBuilder);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly periodPresetDays = PERIOD_PRESET_DAYS;
    protected readonly client = signal<ClientSummary | null>(null);
    protected readonly dashboard = signal<DashboardSnapshot | null>(null);
    protected readonly goals = signal<DietologistClientGoals | null>(null);
    protected readonly recommendations = signal<DietologistRecommendation[]>([]);
    protected readonly loading = signal(true);
    protected readonly detailsLoading = signal(false);
    protected readonly savingRecommendation = signal(false);
    protected readonly error = signal<string | null>(null);
    protected readonly sectionLoadError = signal<string | null>(null);
    protected readonly selectedDateTo = signal(formatDateInputValue(new Date()));
    protected readonly selectedDateFrom = signal(formatDateInputValue(this.addDays(new Date(), -(DEFAULT_PERIOD_DAYS - 1))));
    protected readonly dateFilterForm = this.formBuilder.group({
        dateFrom: [this.selectedDateFrom(), [Validators.required]],
        dateTo: [this.selectedDateTo(), [Validators.required]],
    });
    protected readonly recommendationForm = this.formBuilder.group({
        text: ['', [Validators.required, Validators.maxLength(RECOMMENDATION_MAX_LENGTH)]],
    });
    protected readonly clientTitle = computed(() => {
        const client = this.client();
        if (client === null) {
            return '';
        }

        return getClientDashboardTitle(client);
    });
    protected readonly profileChips = computed(() => {
        return buildClientProfileChips(this.client());
    });
    protected readonly profileDetails = computed<ClientProfileDetail[]>(() => buildClientProfileDetails(this.client()));
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

    protected applyDateFilter(): void {
        const client = this.client();
        const nextPeriod = this.getPeriodFromForm();
        if (
            client === null ||
            this.dateFilterForm.invalid ||
            nextPeriod === null ||
            (nextPeriod.dateFrom === this.selectedDateFrom() && nextPeriod.dateTo === this.selectedDateTo())
        ) {
            this.dateFilterForm.markAllAsTouched();
            return;
        }

        this.selectedDateFrom.set(nextPeriod.dateFrom);
        this.selectedDateTo.set(nextPeriod.dateTo);
        this.reloadDashboard(client);
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
        const client = this.client();
        if (client === null || this.recommendationForm.invalid || this.savingRecommendation()) {
            this.recommendationForm.markAllAsTouched();
            return;
        }

        const text = this.recommendationForm.controls.text.value.trim();
        if (text.length === 0) {
            this.recommendationForm.controls.text.setValue('');
            this.recommendationForm.markAllAsTouched();
            return;
        }

        this.savingRecommendation.set(true);
        this.dietologistFacade
            .createRecommendation(client.userId, { text })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: recommendation => {
                    this.recommendations.update(items => [recommendation, ...items]);
                    this.recommendationForm.reset();
                    this.savingRecommendation.set(false);
                    this.toastService.success(this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.RECOMMENDATIONS.SENT'));
                },
                error: () => {
                    this.savingRecommendation.set(false);
                    this.toastService.error(this.translateService.instant('DIETOLOGIST.CLIENT_DASHBOARD.RECOMMENDATIONS.SEND_ERROR'));
                },
            });
    }

    protected disconnectClient(): void {
        const client = this.client();
        if (client === null) {
            return;
        }

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

    private loadClientDetails(client: ClientSummary): Observable<{
        dashboard: DashboardSnapshot | null;
        goals: DietologistClientGoals | null;
        recommendations: DietologistRecommendation[];
    }> {
        const language = this.translateService.getCurrentLang();
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
        }).pipe(
            tap(result => {
                this.dashboard.set(result.dashboard);
                this.goals.set(result.goals);
                this.recommendations.set(result.recommendations);
            }),
        );
    }

    private reloadDashboard(client: ClientSummary): void {
        if (!this.shouldLoadDashboardSnapshot(client)) {
            this.dashboard.set(null);
            return;
        }

        this.detailsLoading.set(true);
        this.sectionLoadError.set(null);
        this.loadDashboardSnapshot(client, this.translateService.getCurrentLang())
            .pipe(this.handleSectionLoadError<DashboardSnapshot, null>(null))
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                this.dashboard.set(result);
                this.detailsLoading.set(false);
            });
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
        this.dateFilterForm.setValue({ dateFrom, dateTo });
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
        const dateFrom = this.dateFilterForm.controls.dateFrom.value;
        const dateTo = this.dateFilterForm.controls.dateTo.value;
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
}
