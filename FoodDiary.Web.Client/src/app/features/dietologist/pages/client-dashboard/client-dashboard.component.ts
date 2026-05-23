import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { catchError, forkJoin, type Observable, of, switchMap, tap } from 'rxjs';

import { formatDateInputValue, parseLocalDateInputValue } from '../../../../shared/lib/local-date.utils';
import type { ClientSummary, DietologistClientGoals, DietologistRecommendation } from '../../../../shared/models/dietologist.data';
import type { DashboardSnapshot } from '../../../dashboard/models/dashboard.data';
import { DietologistService } from '../../api/dietologist.service';
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
const DAY_STEP = 1;

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
    templateUrl: './client-dashboard.component.html',
    styleUrls: ['./client-dashboard.component.scss'],
})
export class ClientDashboardComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly dietologistService = inject(DietologistService);
    private readonly formBuilder = inject(NonNullableFormBuilder);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly client = signal<ClientSummary | null>(null);
    public readonly dashboard = signal<DashboardSnapshot | null>(null);
    public readonly goals = signal<DietologistClientGoals | null>(null);
    public readonly recommendations = signal<DietologistRecommendation[]>([]);
    public readonly loading = signal(true);
    public readonly detailsLoading = signal(false);
    public readonly savingRecommendation = signal(false);
    public readonly error = signal<string | null>(null);
    public readonly selectedDate = signal(formatDateInputValue(new Date()));
    public readonly dateFilterForm = this.formBuilder.group({
        date: [this.selectedDate(), [Validators.required]],
    });
    public readonly recommendationForm = this.formBuilder.group({
        text: ['', [Validators.required, Validators.maxLength(RECOMMENDATION_MAX_LENGTH)]],
    });
    public readonly clientTitle = computed(() => {
        const client = this.client();
        if (client === null) {
            return '';
        }

        return getClientDashboardTitle(client);
    });
    public readonly profileChips = computed(() => {
        return buildClientProfileChips(this.client());
    });
    public readonly profileDetails = computed<ClientProfileDetail[]>(() => buildClientProfileDetails(this.client()));
    public readonly visibleSections = computed<ClientDashboardSection[]>(() => {
        return buildClientDashboardSections(this.client());
    });
    public readonly hasAnyPermission = computed(() => {
        return this.visibleSections().length > 0;
    });
    public readonly nutritionTiles = computed<ClientMetricTile[]>(() =>
        this.client()?.permissions.shareStatistics === true ? buildNutritionTiles(this.dashboard()) : [],
    );
    public readonly bodyTiles = computed<ClientMetricTile[]>(() => buildBodyTiles(this.dashboard(), this.client()?.permissions));
    public readonly goalTiles = computed<ClientMetricTile[]>(() => buildGoalTiles(this.goals()));
    public readonly mealItems = computed<ClientMealView[]>(() =>
        this.client()?.permissions.shareMeals === true ? buildMealViews(this.dashboard()) : [],
    );
    public readonly weightSummary = computed<ClientBodyMeasurementView | null>(() =>
        this.client()?.permissions.shareWeight === true ? buildWeightView(this.dashboard()) : null,
    );
    public readonly waistSummary = computed<ClientBodyMeasurementView | null>(() =>
        this.client()?.permissions.shareWaist === true ? buildWaistView(this.dashboard()) : null,
    );
    public readonly hydrationSummary = computed<ClientHydrationView | null>(() =>
        this.client()?.permissions.shareHydration === true ? buildHydrationView(this.dashboard()) : null,
    );
    public readonly fastingSummary = computed<ClientFastingView | null>(() =>
        this.client()?.permissions.shareFasting === true ? buildFastingView(this.dashboard()) : null,
    );
    public readonly recommendationItems = computed<ClientRecommendationView[]>(() => buildRecommendationViews(this.recommendations()));

    public constructor() {
        const clientId = this.route.snapshot.paramMap.get('clientId');
        this.dietologistService
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

    public goBack(): void {
        void this.router.navigate(['/dietologist']);
    }

    public applyDateFilter(): void {
        const client = this.client();
        const nextDate = this.dateFilterForm.controls.date.value;
        if (client === null || this.dateFilterForm.invalid || nextDate === this.selectedDate()) {
            this.dateFilterForm.markAllAsTouched();
            return;
        }

        this.selectedDate.set(nextDate);
        this.reloadDashboard(client);
    }

    public showPreviousDay(): void {
        this.shiftSelectedDate(-DAY_STEP);
    }

    public showNextDay(): void {
        this.shiftSelectedDate(DAY_STEP);
    }

    public showToday(): void {
        const today = formatDateInputValue(new Date());
        this.dateFilterForm.controls.date.setValue(today);

        const client = this.client();
        if (client === null || today === this.selectedDate()) {
            return;
        }

        this.selectedDate.set(today);
        this.reloadDashboard(client);
    }

    public submitRecommendation(): void {
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
        this.dietologistService
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

    public disconnectClient(): void {
        const client = this.client();
        if (client === null) {
            return;
        }

        this.dietologistService
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

        return forkJoin({
            dashboard: this.shouldLoadDashboardSnapshot(client)
                ? this.loadDashboardSnapshot(client, language).pipe(catchError(() => of(null)))
                : of(null),
            goals: client.permissions.shareGoals
                ? this.dietologistService.getClientGoals(client.userId).pipe(catchError(() => of(null)))
                : of(null),
            recommendations: this.dietologistService.getRecommendationsForClient(client.userId).pipe(catchError(() => of([]))),
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
        this.loadDashboardSnapshot(client, this.translateService.getCurrentLang())
            .pipe(catchError(() => of(null)))
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                this.dashboard.set(result);
                this.detailsLoading.set(false);
            });
    }

    private loadDashboardSnapshot(client: ClientSummary, language: string): Observable<DashboardSnapshot> {
        return this.dietologistService.getClientDashboard(client.userId, {
            date: this.getSelectedDateValue(),
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

    private shiftSelectedDate(days: number): void {
        const current = this.getSelectedDateValue();
        current.setDate(current.getDate() + days);
        const nextDate = formatDateInputValue(current);
        this.dateFilterForm.controls.date.setValue(nextDate);

        const client = this.client();
        if (client === null) {
            this.selectedDate.set(nextDate);
            return;
        }

        this.selectedDate.set(nextDate);
        this.reloadDashboard(client);
    }

    private getSelectedDateValue(): Date {
        return parseLocalDateInputValue(this.dateFilterForm.controls.date.value) ?? new Date();
    }
}
