import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal, viewChild } from '@angular/core';
import { WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { finalize, firstValueFrom } from 'rxjs';
import { catchError, of } from 'rxjs';

import { MealService } from '../../../features/meals/api/meal.service';
import { Meal } from '../../../features/meals/models/meal.data';
import { AuthService } from '../../../services/auth.service';
import { LocalizationService } from '../../../services/localization.service';
import { NavigationService } from '../../../services/navigation.service';
import { AiFoodService } from '../../../shared/api/ai-food.service';
import { UserService } from '../../../shared/api/user.service';
import { FoodNutritionResponse, FoodVisionItem } from '../../../shared/models/ai.data';
import { ImageSelection } from '../../../shared/models/image-upload.data';
import { AiConsentDialogComponent } from '../ai-consent-dialog/ai-consent-dialog.component';
import { ImageUploadFieldComponent } from '../image-upload-field/image-upload-field.component';
import { PremiumRequiredDialogComponent } from '../premium-required-dialog/premium-required-dialog.component';
import { buildMealManageDtoFromAiResult, mapNutritionItemsToAiInputBarItems } from './ai-input-bar.mapper';
import { AiInputBarMealDetails, AiInputBarMode, AiInputBarResult, AiRecognitionSource } from './ai-input-bar.types';
import { AiPhotoResultComponent } from './ai-photo-result/ai-photo-result.component';

interface AiInputBarChannelState {
    analyzing: WritableSignal<boolean>;
    results: WritableSignal<FoodVisionItem[]>;
    nutritionLoading: WritableSignal<boolean>;
    nutrition: WritableSignal<FoodNutritionResponse | null>;
    errorKey: WritableSignal<string | null>;
    nutritionErrorKey: WritableSignal<string | null>;
}

@Component({
    selector: 'fd-ai-input-bar',
    templateUrl: './ai-input-bar.component.html',
    styleUrls: ['./ai-input-bar.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent, AiPhotoResultComponent, ImageUploadFieldComponent],
})
export class AiInputBarComponent {
    private readonly aiFoodService = inject(AiFoodService);
    private readonly mealService = inject(MealService);
    private readonly userService = inject(UserService);
    private readonly localizationService = inject(LocalizationService);
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly photoUploadField = viewChild(ImageUploadFieldComponent);

    public readonly isProcessing = input<boolean>(false);
    public readonly mode = input<AiInputBarMode>('emit');
    public readonly mealType = input<string | null>(null);
    public readonly mealRecognized = output<AiInputBarResult>();
    public readonly mealCreated = output<Meal>();

    public readonly voiceText = signal('');
    public readonly textSubmittedQuery = signal<string | null>(null);
    public readonly textIsAnalyzing = signal(false);
    public readonly textResults = signal<FoodVisionItem[]>([]);
    public readonly textIsNutritionLoading = signal(false);
    public readonly textNutrition = signal<FoodNutritionResponse | null>(null);
    public readonly textErrorKey = signal<string | null>(null);
    public readonly textNutritionErrorKey = signal<string | null>(null);
    public readonly hasTextResult = computed(() => this.textSubmittedQuery() !== null);
    public readonly isSubmittingMeal = signal(false);
    public readonly isListening = signal(false);
    public readonly isSpeechSupported =
        typeof window !== 'undefined' && ('SpeechRecognition' in window || 'webkitSpeechRecognition' in window);
    private lastTextSource: AiRecognitionSource = 'Text';

    public readonly photoSelection = signal<ImageSelection | null>(null);
    public readonly photoIsAnalyzing = signal(false);
    public readonly photoResults = signal<FoodVisionItem[]>([]);
    public readonly photoIsNutritionLoading = signal(false);
    public readonly photoNutrition = signal<FoodNutritionResponse | null>(null);
    public readonly photoErrorKey = signal<string | null>(null);
    public readonly photoNutritionErrorKey = signal<string | null>(null);
    public readonly hasPhotoResult = computed(() => this.photoSelection() !== null);
    public readonly hasAttachedResult = computed(() => this.hasTextResult() || this.hasPhotoResult());

    public readonly isDisabled = computed(
        () =>
            this.isProcessing() ||
            this.textIsAnalyzing() ||
            this.textIsNutritionLoading() ||
            this.photoIsAnalyzing() ||
            this.photoIsNutritionLoading() ||
            this.isSubmittingMeal(),
    );

    public readonly showDetails = computed(() => this.mode() === 'create');
    public readonly submitLabelKey = computed(() =>
        this.mode() === 'create' ? 'CONSUMPTION_LIST.VOICE_CREATE_MEAL' : 'AI_INPUT_BAR.ADD_ACTION',
    );

    private speechRecognition: unknown = null;

    public onTextInput(event: Event): void {
        this.voiceText.set((event.target as HTMLInputElement).value);
    }

    public async submitText(source: AiRecognitionSource = 'Text'): Promise<void> {
        const text = this.voiceText().trim();
        if (!text || this.isDisabled()) {
            return;
        }

        if (!this.ensurePremium()) {
            return;
        }

        if (!(await this.ensureAiConsent())) {
            return;
        }

        this.dismissPhotoResult();
        this.dismissTextResult();
        this.lastTextSource = source;
        this.textSubmittedQuery.set(text);
        this.runTextAnalysis(text);
    }

    public async toggleMic(): Promise<void> {
        if (this.isListening()) {
            this.stopListening();
            return;
        }

        if (!this.ensurePremium()) {
            return;
        }

        if (!(await this.ensureAiConsent())) {
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
                void this.submitText('Voice');
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

    public onTextAddToMeal(details: AiInputBarMealDetails): void {
        const nutrition = this.textNutrition();
        if (!nutrition) {
            return;
        }

        const results = this.textResults();
        this.submitMeal({
            source: this.lastTextSource,
            mealType: this.mealType(),
            recognizedAtUtc: new Date().toISOString(),
            notes: this.textSubmittedQuery(),
            date: details.date,
            time: details.time,
            comment: details.comment ?? null,
            preMealSatietyLevel: details.preMealSatietyLevel ?? null,
            postMealSatietyLevel: details.postMealSatietyLevel ?? null,
            items: mapNutritionItemsToAiInputBarItems(nutrition, results),
        });
    }

    public async onPhotoClick(): Promise<void> {
        if (this.isDisabled()) {
            return;
        }

        if (!this.ensurePremium()) {
            return;
        }

        if (!(await this.ensureAiConsent())) {
            return;
        }

        this.dismissTextResult();
        this.photoUploadField()?.openFilePicker();
    }

    public onPhotoSelected(selection: ImageSelection | null): void {
        if (!selection?.assetId) {
            return;
        }

        this.photoSelection.set(selection);
        this.photoErrorKey.set(null);
        this.photoResults.set([]);
        this.photoNutrition.set(null);
        this.photoNutritionErrorKey.set(null);
        this.runPhotoAnalysis(selection.assetId);
    }

    public onPhotoAddToMeal(details: AiInputBarMealDetails): void {
        const nutrition = this.photoNutrition();
        if (!nutrition) {
            return;
        }

        const selection = this.photoSelection();
        const results = this.photoResults();

        this.submitMeal({
            source: 'Photo',
            mealType: this.mealType(),
            imageAssetId: selection?.assetId ?? null,
            imageUrl: selection?.url ?? null,
            recognizedAtUtc: new Date().toISOString(),
            notes: nutrition.notes ?? null,
            date: details.date,
            time: details.time,
            comment: details.comment ?? null,
            preMealSatietyLevel: details.preMealSatietyLevel ?? null,
            postMealSatietyLevel: details.postMealSatietyLevel ?? null,
            items: mapNutritionItemsToAiInputBarItems(nutrition, results),
        });
    }

    public dismissTextResult(): void {
        this.textSubmittedQuery.set(null);
        this.textIsAnalyzing.set(false);
        this.textResults.set([]);
        this.textIsNutritionLoading.set(false);
        this.textNutrition.set(null);
        this.textErrorKey.set(null);
        this.textNutritionErrorKey.set(null);
    }

    public onTextEditApplied(items: FoodVisionItem[]): void {
        this.textResults.set(items);
        this.runTextNutrition(items);
    }

    public onTextReanalyze(): void {
        const query = this.textSubmittedQuery();
        if (!query || this.textIsAnalyzing()) {
            return;
        }

        this.runTextAnalysis(query);
    }

    public dismissPhotoResult(): void {
        this.photoSelection.set(null);
        this.photoIsAnalyzing.set(false);
        this.photoResults.set([]);
        this.photoIsNutritionLoading.set(false);
        this.photoNutrition.set(null);
        this.photoErrorKey.set(null);
        this.photoNutritionErrorKey.set(null);
    }

    public onPhotoEditApplied(items: FoodVisionItem[]): void {
        this.photoResults.set(items);
        this.runPhotoNutrition(items);
    }

    public onPhotoReanalyze(): void {
        const assetId = this.photoSelection()?.assetId;
        if (!assetId || this.photoIsAnalyzing()) {
            return;
        }

        this.photoErrorKey.set(null);
        this.photoResults.set([]);
        this.photoNutrition.set(null);
        this.photoNutritionErrorKey.set(null);
        this.runPhotoAnalysis(assetId);
    }

    public clearState(): void {
        this.voiceText.set('');
        this.dismissTextResult();
        this.dismissPhotoResult();
    }

    private submitMeal(result: AiInputBarResult): void {
        if (this.mode() === 'emit') {
            this.mealRecognized.emit(result);
            return;
        }

        const mealDate = result.date && result.time ? new Date(`${result.date}T${result.time}`) : new Date();
        this.isSubmittingMeal.set(true);
        this.mealService
            .create(buildMealManageDtoFromAiResult(result, mealDate))
            .pipe(
                finalize(() => this.isSubmittingMeal.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: (meal: Meal | null) => {
                    if (!meal) {
                        return;
                    }

                    this.clearState();
                    this.mealCreated.emit(meal);
                },
            });
    }

    private runTextAnalysis(text: string): void {
        this.runAnalysisRequest(
            this.textState(),
            this.aiFoodService.parseFoodText({ text }),
            {
                premium: 'AI_INPUT_BAR.TEXT_ERROR_PREMIUM',
                quota: 'AI_INPUT_BAR.TEXT_ERROR_QUOTA',
                generic: 'AI_INPUT_BAR.TEXT_ERROR_GENERIC',
            },
            items => this.runTextNutrition(items),
        );
    }

    private runTextNutrition(items: FoodVisionItem[]): void {
        this.runNutritionRequest(this.textState(), items, {
            quota: 'AI_INPUT_BAR.TEXT_ERROR_QUOTA',
            generic: 'AI_INPUT_BAR.TEXT_NUTRITION_ERROR',
        });
    }

    private runPhotoAnalysis(assetId: string): void {
        this.runAnalysisRequest(
            this.photoState(),
            this.aiFoodService.analyzeFoodImage({ imageAssetId: assetId }),
            {
                premium: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_PREMIUM',
                quota: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA',
                generic: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_GENERIC',
            },
            items => this.runPhotoNutrition(items),
        );
    }

    private runPhotoNutrition(items: FoodVisionItem[]): void {
        this.runNutritionRequest(this.photoState(), items, {
            quota: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA',
            generic: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.NUTRITION_ERROR',
        });
    }

    private ensurePremium(): boolean {
        if (this.authService.isPremium()) {
            return true;
        }

        this.fdDialogService
            .open<PremiumRequiredDialogComponent, never, boolean>(PremiumRequiredDialogComponent, { preset: 'confirm' })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(confirmed => {
                if (confirmed) {
                    void this.navigationService.navigateToPremiumAccess();
                }
            });

        return false;
    }

    private async ensureAiConsent(): Promise<boolean> {
        const cachedUser = this.userService.user();
        if (cachedUser?.aiConsentAcceptedAt) {
            return true;
        }

        const freshUser = await firstValueFrom(this.userService.getInfoSilently());
        if (freshUser?.aiConsentAcceptedAt) {
            return true;
        }

        const accepted = await firstValueFrom(
            this.fdDialogService
                .open<AiConsentDialogComponent, never, boolean>(AiConsentDialogComponent, {
                    size: 'lg',
                    disableClose: true,
                })
                .afterClosed(),
        );

        if (!accepted) {
            return false;
        }

        await firstValueFrom(this.userService.acceptAiConsent());
        return true;
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

    private textState(): AiInputBarChannelState {
        return {
            analyzing: this.textIsAnalyzing,
            results: this.textResults,
            nutritionLoading: this.textIsNutritionLoading,
            nutrition: this.textNutrition,
            errorKey: this.textErrorKey,
            nutritionErrorKey: this.textNutritionErrorKey,
        };
    }

    private photoState(): AiInputBarChannelState {
        return {
            analyzing: this.photoIsAnalyzing,
            results: this.photoResults,
            nutritionLoading: this.photoIsNutritionLoading,
            nutrition: this.photoNutrition,
            errorKey: this.photoErrorKey,
            nutritionErrorKey: this.photoNutritionErrorKey,
        };
    }

    private runAnalysisRequest(
        state: AiInputBarChannelState,
        request$: ReturnType<AiFoodService['parseFoodText']> | ReturnType<AiFoodService['analyzeFoodImage']>,
        errorKeys: { premium: string; quota: string; generic: string },
        onItems: (items: FoodVisionItem[]) => void,
    ): void {
        state.analyzing.set(true);
        state.results.set([]);
        state.nutrition.set(null);
        state.errorKey.set(null);
        state.nutritionErrorKey.set(null);

        request$
            .pipe(
                catchError(err => {
                    if (err?.status === 403) {
                        state.errorKey.set(errorKeys.premium);
                    } else if (err?.status === 429) {
                        state.errorKey.set(errorKeys.quota);
                    } else {
                        state.errorKey.set(errorKeys.generic);
                    }
                    return of(null);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(response => {
                state.analyzing.set(false);
                if (!response) {
                    return;
                }

                const items = response.items ?? [];
                state.results.set(items);
                if (items.length) {
                    onItems(items);
                }
            });
    }

    private runNutritionRequest(
        state: AiInputBarChannelState,
        items: FoodVisionItem[],
        errorKeys: { quota: string; generic: string },
    ): void {
        state.nutritionLoading.set(true);
        state.nutrition.set(null);
        state.nutritionErrorKey.set(null);

        this.aiFoodService
            .calculateNutrition({ items })
            .pipe(
                catchError(err => {
                    if (err?.status === 429) {
                        state.nutritionErrorKey.set(errorKeys.quota);
                    } else {
                        state.nutritionErrorKey.set(errorKeys.generic);
                    }
                    return of(null);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(response => {
                state.nutritionLoading.set(false);
                if (!response) {
                    return;
                }

                state.nutrition.set(response);
            });
    }
}
