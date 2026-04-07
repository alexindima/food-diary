import { BreakpointObserver } from '@angular/cdk/layout';
import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDateRangeInputComponent, FdUiDateRangeValue } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { ExportService } from '../../api/export.service';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { MatIconModule } from '@angular/material/icon';
import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { Observable, catchError, debounceTime, distinctUntilChanged, finalize, map, of, startWith, switchMap } from 'rxjs';

import { AiFoodService } from '../../../../shared/api/ai-food.service';
import { LocalizationService } from '../../../../services/localization.service';
import { FoodVisionResponse } from '../../../../shared/models/ai.data';
import { MealService } from '../../api/meal.service';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import { MealDetailActionResult, MealDetailComponent } from '../../components/detail/meal-detail.component';
import { FavoriteMeal, Meal, MealFilters } from '../../models/meal.data';
import { MealCardComponent } from '../../../../components/shared/meal-card/meal-card.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { LocalizedDatePipe } from '../../../../pipes/localized-date.pipe';
import { NavigationService } from '../../../../services/navigation.service';
import { FormGroupControls } from '../../../../shared/lib/common.data';
import { PagedData } from '../../../../shared/lib/paged-data.data';

@Component({
    selector: 'fd-meal-list',
    templateUrl: './meal-list.component.html',
    styleUrls: ['./meal-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        DecimalPipe,
        ReactiveFormsModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDateRangeInputComponent,
        FdUiPaginationComponent,
        SkeletonCardComponent,
        ErrorStateComponent,
        FdUiIconModule,
        MatIconModule,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        MealCardComponent,
        LocalizedDatePipe,
    ],
})
export class MealListComponent implements OnInit {
    private readonly mealService = inject(MealService);
    private readonly favoriteMealService = inject(FavoriteMealService);
    private readonly aiFoodService = inject(AiFoodService);
    private readonly localizationService = inject(LocalizationService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly exportService = inject(ExportService);
    private readonly breakpointObserver = inject(BreakpointObserver);

    public searchForm: FormGroup<SearchFormGroup>;
    public consumptionData: PagedData<Meal> = new PagedData<Meal>();
    public currentPageIndex = 0;
    public readonly groupedConsumptions = computed(() => this.groupByDate(this.consumptionData.items()));
    public readonly errorKey = signal<string | null>(null);
    public readonly favorites = signal<FavoriteMeal[]>([]);
    public readonly isFavoritesOpen = signal(false);
    public readonly voiceText = signal('');
    public readonly isVoiceLoading = signal(false);
    public readonly voiceResult = signal<FoodVisionResponse | null>(null);
    public readonly isListening = signal(false);
    public readonly isSpeechSupported =
        typeof window !== 'undefined' && ('SpeechRecognition' in window || 'webkitSpeechRecognition' in window);
    private speechRecognition: unknown = null;
    public readonly isMobileView = signal<boolean>(window.matchMedia('(max-width: 768px)').matches);
    private readonly isMobileDateFilterOpen = signal(false);
    private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

    public constructor() {
        this.searchForm = new FormGroup<SearchFormGroup>({
            dateRange: new FormControl<FdUiDateRangeValue | null>(null),
        });
    }

    public ngOnInit(): void {
        this.breakpointObserver
            .observe('(max-width: 768px)')
            .pipe(
                map(result => result.matches),
                distinctUntilChanged(),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(isMobile => {
                this.isMobileView.set(isMobile);
                if (!isMobile) {
                    this.isMobileDateFilterOpen.set(false);
                }
            });

        this.searchForm.valueChanges
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                debounceTime(300),
                startWith(this.searchForm.value),
                switchMap(() => this.loadConsumptions(1)),
            )
            .subscribe();

        this.loadFavorites();
    }

    public loadFavorites(): void {
        this.favoriteMealService
            .getAll()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(favorites => {
                this.favorites.set(favorites);
            });
    }

    public toggleFavorites(): void {
        this.isFavoritesOpen.update(v => !v);
    }

    public repeatFavorite(favorite: FavoriteMeal): void {
        const today = new Date().toISOString().slice(0, 10);
        this.mealService.repeat(favorite.mealId, today).subscribe({
            next: () => {
                this.scrollToTop();
                this.loadConsumptions(this.currentPageIndex + 1).subscribe();
            },
        });
    }

    public removeFavorite(favorite: FavoriteMeal): void {
        this.favoriteMealService.remove(favorite.id).subscribe({
            next: () => this.favorites.update(list => list.filter(f => f.id !== favorite.id)),
        });
    }

    public onVoiceTextInput(event: Event): void {
        this.voiceText.set((event.target as HTMLInputElement).value);
    }

    public submitVoiceText(): void {
        const text = this.voiceText().trim();
        if (!text || this.isVoiceLoading()) {
            return;
        }

        this.isVoiceLoading.set(true);
        this.aiFoodService
            .parseFoodText({ text })
            .pipe(
                finalize(() => this.isVoiceLoading.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: result => this.voiceResult.set(result),
                error: () => this.voiceResult.set(null),
            });
    }

    public toggleMic(): void {
        if (this.isListening()) {
            this.stopListening();
            return;
        }

        const SpeechRecognitionCtor =
            (window as unknown as Record<string, unknown>)['SpeechRecognition'] ??
            (window as unknown as Record<string, unknown>)['webkitSpeechRecognition'];
        if (!SpeechRecognitionCtor) {
            return;
        }

        const recognition = new (SpeechRecognitionCtor as { new (): Record<string, unknown> })();
        const lang = this.localizationService.getCurrentLanguage();
        recognition['lang'] = lang === 'ru' ? 'ru-RU' : 'en-US';
        recognition['interimResults'] = false;
        recognition['maxAlternatives'] = 1;

        recognition['onresult'] = (event: Record<string, unknown>): void => {
            const results = event['results'] as { [key: number]: { [key: number]: { transcript: string } } } | undefined;
            const transcript = results?.[0]?.[0]?.transcript;
            if (transcript) {
                this.voiceText.set(transcript);
                this.submitVoiceText();
            }
        };

        recognition['onerror'] = (): void => {
            this.isListening.set(false);
        };

        recognition['onend'] = (): void => {
            this.isListening.set(false);
            this.speechRecognition = null;
        };

        this.speechRecognition = recognition;
        this.isListening.set(true);
        (recognition['start'] as () => void)();
    }

    private stopListening(): void {
        if (this.speechRecognition) {
            const stopFn = (this.speechRecognition as Record<string, unknown>)['stop'];
            if (typeof stopFn === 'function') {
                stopFn.call(this.speechRecognition);
            }
            this.speechRecognition = null;
        }
        this.isListening.set(false);
    }

    public dismissVoiceResult(): void {
        this.voiceResult.set(null);
    }

    public createMealFromVoice(): void {
        const result = this.voiceResult();
        if (!result?.items.length) {
            return;
        }

        this.isVoiceLoading.set(true);
        this.aiFoodService
            .calculateNutrition({ items: result.items })
            .pipe(
                finalize(() => this.isVoiceLoading.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: nutrition => {
                    const now = new Date();

                    this.mealService
                        .create({
                            date: now,
                            isNutritionAutoCalculated: false,
                            manualCalories: nutrition.calories,
                            manualProteins: nutrition.protein,
                            manualFats: nutrition.fat,
                            manualCarbs: nutrition.carbs,
                            manualFiber: nutrition.fiber,
                            manualAlcohol: nutrition.alcohol,
                            items: [],
                            aiSessions: [
                                {
                                    recognizedAtUtc: now.toISOString(),
                                    notes: this.voiceText(),
                                    items: nutrition.items.map(item => ({
                                        nameEn: item.name,
                                        amount: item.amount,
                                        unit: item.unit,
                                        calories: item.calories,
                                        proteins: item.protein,
                                        fats: item.fat,
                                        carbs: item.carbs,
                                        fiber: item.fiber,
                                        alcohol: item.alcohol,
                                    })),
                                },
                            ],
                        })
                        .subscribe({
                            next: () => {
                                this.voiceResult.set(null);
                                this.voiceText.set('');
                                this.scrollToTop();
                                this.loadConsumptions(1).subscribe();
                            },
                        });
                },
            });
    }

    public loadConsumptions(page: number): Observable<void> {
        this.consumptionData.setLoading(true);
        const dateRange = this.searchForm.controls.dateRange.value;

        const filters: MealFilters = {
            dateFrom: this.toIsoDate(dateRange?.start ?? null),
            dateTo: this.toIsoDate(dateRange?.end ?? null),
        };

        return this.mealService.query(page, 10, filters).pipe(
            map(pageData => {
                this.consumptionData.setData(pageData);
                this.currentPageIndex = pageData.page - 1;
                this.consumptionData.setLoading(false);
                this.errorKey.set(null);
            }),
            catchError(() => {
                this.consumptionData.clearData();
                this.consumptionData.setLoading(false);
                this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
                return of();
            }),
        );
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.currentPageIndex = pageIndex;
        this.loadConsumptions(pageIndex + 1).subscribe();
    }

    public async openMealDetails(consumption: Meal): Promise<void> {
        this.fdDialogService
            .open<MealDetailComponent, Meal, MealDetailActionResult>(MealDetailComponent, {
                size: 'lg',
                data: consumption,
            })
            .afterClosed()
            .subscribe(data => {
                this.loadFavorites();

                if (!data) {
                    return;
                }

                if (data.action === 'Edit') {
                    this.navigationService.navigateToConsumptionEdit(data.id);
                } else if (data.action === 'Repeat') {
                    const today = new Date().toISOString().slice(0, 10);
                    this.mealService.repeat(data.id, today).subscribe({
                        next: () => {
                            this.scrollToTop();
                            this.loadConsumptions(this.currentPageIndex + 1).subscribe();
                        },
                    });
                } else if (data.action === 'Delete') {
                    this.mealService.deleteById(data.id).subscribe({
                        next: () => {
                            this.scrollToTop();
                            this.loadConsumptions(this.currentPageIndex + 1).subscribe();
                        },
                    });
                }
            });
    }

    public async goToMealAdd(): Promise<void> {
        await this.navigationService.navigateToConsumptionAdd();
    }

    public exportCsv(): void {
        this.exportDiary('csv');
    }

    public exportPdf(): void {
        this.exportDiary('pdf');
    }

    private exportDiary(format: 'csv' | 'pdf'): void {
        const dateRange = this.searchForm.controls.dateRange.value;
        const now = new Date();
        const thirtyDaysAgo = new Date(now.getFullYear(), now.getMonth(), now.getDate() - 30);
        const dateFrom = this.toIsoDate(dateRange?.start ?? thirtyDaysAgo) ?? new Date().toISOString();
        const dateTo = this.toIsoDate(dateRange?.end ?? now) ?? new Date().toISOString();
        this.exportService.exportDiary(dateFrom, dateTo, format);
    }

    public toggleMobileDateFilter(): void {
        this.isMobileDateFilterOpen.update(value => !value);
    }

    public get hasDateFilter(): boolean {
        const dateRange = this.searchForm.controls.dateRange.value;
        return !!dateRange?.start || !!dateRange?.end;
    }

    public get isMobileDateFilterVisible(): boolean {
        return this.isMobileDateFilterOpen() || this.hasDateFilter;
    }

    public get isEmptyState(): boolean {
        return this.consumptionData.items().length === 0 && !this.hasDateFilter;
    }

    public get isNoResultsState(): boolean {
        return this.consumptionData.items().length === 0 && this.hasDateFilter;
    }

    protected scrollToTop(): void {
        this.container().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    private toIsoDate(date: Date | null | undefined): string | undefined {
        if (!date) {
            return undefined;
        }

        const normalized = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
        return normalized.toISOString();
    }

    private groupByDate(items: Meal[]): { date: Date; items: Meal[] }[] {
        const buckets = new Map<string, { date: Date; items: Meal[] }>();

        for (const item of items) {
            const date = new Date(item.date);
            const key = date.toISOString().slice(0, 10);
            if (!buckets.has(key)) {
                buckets.set(key, { date, items: [] });
            }
            buckets.get(key)!.items.push(item);
        }

        return Array.from(buckets.values()).sort((a, b) => b.date.getTime() - a.date.getTime());
    }
}

interface SearchFormValues {
    dateRange: FdUiDateRangeValue | null;
}

type SearchFormGroup = FormGroupControls<SearchFormValues>;
