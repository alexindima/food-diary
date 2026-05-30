import { isPlatformBrowser } from '@angular/common';
import { HttpStatusCode } from '@angular/common/http';
import type { WritableSignal } from '@angular/core';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    inject,
    input,
    output,
    PLATFORM_ID,
    signal,
    viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { catchError, firstValueFrom, type Observable, of } from 'rxjs';

import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { LocalizationService } from '../../../shared/i18n/localization.service';
import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import { getNumberProperty } from '../../../shared/lib/unknown-value.utils';
import { UserFacade } from '../../../shared/lib/user.facade';
import type { FoodNutritionResponse, FoodVisionItem, FoodVisionResponse } from '../../../shared/models/ai.data';
import type { ImageSelection } from '../../../shared/models/image-upload.data';
import { AiConsentDialogComponent } from '../ai-consent-dialog/ai-consent-dialog';
import { ImageUploadFieldComponent } from '../image-upload-field/image-upload-field';
import { PremiumRequiredDialogComponent } from '../premium-required-dialog/premium-required-dialog';
import { buildPhotoAiInputBarResult, buildTextAiInputBarResult } from './ai-input-bar.mapper';
import type { AiInputBarMealDetails, AiInputBarMode, AiInputBarResult, AiRecognitionSource } from './ai-input-bar.types';
import { AiPhotoResultComponent } from './ai-photo-result/ai-photo-result';
import type { AiPhotoEditApplied } from './ai-photo-result/ai-photo-result-lib/ai-photo-result.types';

type AiInputBarChannelState = {
    analyzing: WritableSignal<boolean>;
    results: WritableSignal<FoodVisionItem[]>;
    nutritionLoading: WritableSignal<boolean>;
    nutrition: WritableSignal<FoodNutritionResponse | null>;
    errorKey: WritableSignal<string | null>;
    nutritionErrorKey: WritableSignal<string | null>;
};

type SpeechRecognitionAlternativeLike = {
    transcript: string;
};

type SpeechRecognitionResultEventLike = {
    results: ArrayLike<ArrayLike<SpeechRecognitionAlternativeLike>>;
};

type SpeechRecognitionLike = {
    lang: string;
    interimResults: boolean;
    maxAlternatives: number;
    onresult: (event: SpeechRecognitionResultEventLike) => void;
    onerror: () => void;
    onend: () => void;
    start: () => void;
    stop: () => void;
};

type SpeechRecognitionConstructorLike = {
    new (): SpeechRecognitionLike;
};

declare global {
    interface Window {
        SpeechRecognition?: SpeechRecognitionConstructorLike;
        webkitSpeechRecognition?: SpeechRecognitionConstructorLike;
    }
}

@Component({
    selector: 'fd-ai-input-bar',
    templateUrl: './ai-input-bar.html',
    styleUrls: ['./ai-input-bar.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent, AiPhotoResultComponent, ImageUploadFieldComponent],
})
export class AiInputBarComponent {
    private readonly aiFoodFacade = inject(AiFoodFacade);
    private readonly userFacade = inject(UserFacade);
    private readonly localizationService = inject(LocalizationService);
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly isBrowser = isPlatformBrowser(this.platformId);
    private readonly photoUploadField = viewChild(ImageUploadFieldComponent);

    public readonly isProcessing = input<boolean>(false);
    public readonly clearToken = input(0);
    public readonly mode = input<AiInputBarMode>('emit');
    public readonly mealType = input<string | null>(null);
    public readonly mealRecognized = output<AiInputBarResult>();
    public readonly mealCreateRequested = output<AiInputBarResult>();

    protected readonly voiceText = signal('');
    protected readonly textSubmittedQuery = signal<string | null>(null);
    protected readonly textIsAnalyzing = signal(false);
    protected readonly textResults = signal<FoodVisionItem[]>([]);
    protected readonly textIsNutritionLoading = signal(false);
    protected readonly textNutrition = signal<FoodNutritionResponse | null>(null);
    protected readonly textErrorKey = signal<string | null>(null);
    protected readonly textNutritionErrorKey = signal<string | null>(null);
    protected readonly hasTextResult = computed(() => this.textSubmittedQuery() !== null);
    protected readonly isSubmittingMeal = signal(false);
    protected readonly isListening = signal(false);
    protected readonly isSpeechSupported = this.isBrowser && ('SpeechRecognition' in window || 'webkitSpeechRecognition' in window);
    private lastTextSource: AiRecognitionSource = 'Text';

    protected readonly photoSelection = signal<ImageSelection | null>(null);
    protected readonly photoIsAnalyzing = signal(false);
    protected readonly photoResults = signal<FoodVisionItem[]>([]);
    protected readonly photoIsNutritionLoading = signal(false);
    protected readonly photoNutrition = signal<FoodNutritionResponse | null>(null);
    protected readonly photoErrorKey = signal<string | null>(null);
    protected readonly photoNutritionErrorKey = signal<string | null>(null);
    protected readonly hasPhotoResult = computed(() => this.photoSelection() !== null);
    protected readonly hasAttachedResult = computed(() => this.hasTextResult() || this.hasPhotoResult());
    protected readonly microphoneIcon = computed(() => (this.isListening() ? 'mic' : 'mic_none'));
    protected readonly textSubmitIcon = computed(() =>
        this.textIsAnalyzing() || this.textIsNutritionLoading() ? 'hourglass_empty' : 'send',
    );

    protected readonly isDisabled = computed(
        () =>
            this.isProcessing() ||
            this.textIsAnalyzing() ||
            this.textIsNutritionLoading() ||
            this.photoIsAnalyzing() ||
            this.photoIsNutritionLoading() ||
            this.isSubmittingMeal(),
    );

    protected readonly showDetails = computed(() => this.mode() === 'create');
    protected readonly submitLabelKey = computed(() =>
        this.mode() === 'create' ? 'CONSUMPTION_LIST.VOICE_CREATE_MEAL' : 'AI_INPUT_BAR.ADD_ACTION',
    );

    private speechRecognition: SpeechRecognitionLike | null = null;

    public constructor() {
        effect(() => {
            if (this.clearToken() > 0) {
                this.clearState();
            }
        });
    }

    protected onTextInput(event: Event): void {
        if (event.target instanceof HTMLInputElement) {
            this.voiceText.set(event.target.value);
        }
    }

    protected async submitTextAsync(source: AiRecognitionSource = 'Text'): Promise<void> {
        const text = this.voiceText().trim();
        if (text.length === 0 || this.isDisabled()) {
            return;
        }

        if (!this.ensurePremium()) {
            return;
        }

        if (!(await this.ensureAiConsentAsync())) {
            return;
        }

        this.dismissPhotoResult();
        this.dismissTextResult();
        this.lastTextSource = source;
        this.textSubmittedQuery.set(text);
        this.runTextAnalysis(text);
    }

    protected async toggleMicAsync(): Promise<void> {
        if (this.isListening()) {
            this.stopListening();
            return;
        }

        if (!this.ensurePremium()) {
            return;
        }

        if (!(await this.ensureAiConsentAsync())) {
            return;
        }

        if (!this.isSpeechSupported) {
            return;
        }

        const SpeechRecognitionCtor = window.SpeechRecognition ?? window.webkitSpeechRecognition;
        if (SpeechRecognitionCtor === undefined) {
            return;
        }

        const recognition = new SpeechRecognitionCtor();
        recognition.lang = resolveAppLocale(this.localizationService.getCurrentLanguage());
        recognition.interimResults = false;
        recognition.maxAlternatives = 1;

        recognition.onresult = (event: SpeechRecognitionResultEventLike): void => {
            const transcript = event.results[0][0].transcript;
            if (transcript.length > 0) {
                this.voiceText.set(transcript);
                void this.submitTextAsync('Voice');
            }
        };

        recognition.onerror = (): void => {
            this.isListening.set(false);
        };

        recognition.onend = (): void => {
            this.isListening.set(false);
            this.speechRecognition = null;
        };

        this.speechRecognition = recognition;
        this.isListening.set(true);
        recognition.start();
    }

    protected onTextAddToMeal(details: AiInputBarMealDetails): void {
        const nutrition = this.textNutrition();
        if (nutrition === null) {
            return;
        }

        this.submitMeal(
            buildTextAiInputBarResult({
                source: this.lastTextSource,
                mealType: this.mealType(),
                recognizedAtUtc: new Date().toISOString(),
                query: this.textSubmittedQuery(),
                details,
                nutrition,
                results: this.textResults(),
            }),
        );
    }

    protected async onPhotoClickAsync(): Promise<void> {
        if (this.isDisabled()) {
            return;
        }

        if (!this.ensurePremium()) {
            return;
        }

        if (!(await this.ensureAiConsentAsync())) {
            return;
        }

        this.dismissTextResult();
        this.photoUploadField()?.openFilePicker();
    }

    protected onPhotoSelected(selection: ImageSelection | null): void {
        if (selection?.assetId === null || selection?.assetId === undefined) {
            return;
        }

        this.photoSelection.set(selection);
        this.photoErrorKey.set(null);
        this.photoResults.set([]);
        this.photoNutrition.set(null);
        this.photoNutritionErrorKey.set(null);
        this.runPhotoAnalysis(selection.assetId);
    }

    protected onPhotoAddToMeal(details: AiInputBarMealDetails): void {
        const nutrition = this.photoNutrition();
        if (nutrition === null) {
            return;
        }

        const selection = this.photoSelection();
        this.submitMeal(
            buildPhotoAiInputBarResult({
                mealType: this.mealType(),
                selection,
                recognizedAtUtc: new Date().toISOString(),
                details,
                nutrition,
                results: this.photoResults(),
            }),
        );
    }

    protected dismissTextResult(): void {
        this.textSubmittedQuery.set(null);
        this.textIsAnalyzing.set(false);
        this.textResults.set([]);
        this.textIsNutritionLoading.set(false);
        this.textNutrition.set(null);
        this.textErrorKey.set(null);
        this.textNutritionErrorKey.set(null);
    }

    protected onTextEditApplied(result: AiPhotoEditApplied): void {
        this.textResults.set(result.items);
        if (result.items.length === 0) {
            this.setEmptyItemsError(this.textState());
            return;
        }

        if (result.nutrition !== null) {
            this.textIsNutritionLoading.set(false);
            this.textNutritionErrorKey.set(null);
            this.textNutrition.set(result.nutrition);
            return;
        }

        this.runTextNutrition(result.items);
    }

    protected onTextReanalyze(): void {
        const query = this.textSubmittedQuery();
        if (query === null || query.length === 0 || this.textIsAnalyzing()) {
            return;
        }

        this.runTextAnalysis(query);
    }

    protected dismissPhotoResult(): void {
        this.photoSelection.set(null);
        this.photoIsAnalyzing.set(false);
        this.photoResults.set([]);
        this.photoIsNutritionLoading.set(false);
        this.photoNutrition.set(null);
        this.photoErrorKey.set(null);
        this.photoNutritionErrorKey.set(null);
    }

    protected onPhotoEditApplied(result: AiPhotoEditApplied): void {
        this.photoResults.set(result.items);
        if (result.items.length === 0) {
            this.setEmptyItemsError(this.photoState());
            return;
        }

        if (result.nutrition !== null) {
            this.photoIsNutritionLoading.set(false);
            this.photoNutritionErrorKey.set(null);
            this.photoNutrition.set(result.nutrition);
            return;
        }

        this.runPhotoNutrition(result.items);
    }

    protected onPhotoReanalyze(): void {
        const assetId = this.photoSelection()?.assetId;
        if (assetId === null || assetId === undefined || this.photoIsAnalyzing()) {
            return;
        }

        this.photoErrorKey.set(null);
        this.photoResults.set([]);
        this.photoNutrition.set(null);
        this.photoNutritionErrorKey.set(null);
        this.runPhotoAnalysis(assetId);
    }

    protected clearState(): void {
        this.voiceText.set('');
        this.dismissTextResult();
        this.dismissPhotoResult();
    }

    private submitMeal(result: AiInputBarResult): void {
        if (this.mode() === 'emit') {
            this.mealRecognized.emit(result);
            this.clearState();
            return;
        }

        this.mealCreateRequested.emit(result);
    }

    private runTextAnalysis(text: string): void {
        this.runAnalysisRequest(
            this.textState(),
            this.aiFoodFacade.parseFoodText({ text }),
            {
                premium: 'AI_INPUT_BAR.TEXT_ERROR_PREMIUM',
                quota: 'AI_INPUT_BAR.TEXT_ERROR_QUOTA',
                generic: 'AI_INPUT_BAR.TEXT_ERROR_GENERIC',
            },
            items => {
                this.runTextNutrition(items);
            },
        );
    }

    private runTextNutrition(items: FoodVisionItem[]): void {
        if (items.length === 0) {
            this.setEmptyItemsError(this.textState());
            return;
        }

        this.runNutritionRequest(this.textState(), items, {
            quota: 'AI_INPUT_BAR.TEXT_ERROR_QUOTA',
            generic: 'AI_INPUT_BAR.TEXT_NUTRITION_ERROR',
        });
    }

    private runPhotoAnalysis(assetId: string): void {
        this.runAnalysisRequest(
            this.photoState(),
            this.aiFoodFacade.analyzeFoodImage({ imageAssetId: assetId }),
            {
                premium: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_PREMIUM',
                quota: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA',
                generic: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_GENERIC',
            },
            items => {
                this.runPhotoNutrition(items);
            },
        );
    }

    private runPhotoNutrition(items: FoodVisionItem[]): void {
        if (items.length === 0) {
            this.setEmptyItemsError(this.photoState());
            return;
        }

        this.runNutritionRequest(this.photoState(), items, {
            quota: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA',
            generic: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.NUTRITION_ERROR',
        });
    }

    private setEmptyItemsError(state: AiInputBarChannelState): void {
        state.results.set([]);
        state.nutritionLoading.set(false);
        state.nutrition.set(null);
        state.errorKey.set('AI_INPUT_BAR.EMPTY_ITEMS_ERROR');
        state.nutritionErrorKey.set(null);
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
                if (confirmed === true) {
                    void this.navigationService.navigateToPremiumAccessAsync();
                }
            });

        return false;
    }

    private async ensureAiConsentAsync(): Promise<boolean> {
        const cachedUser = this.userFacade.user();
        if (cachedUser?.aiConsentAcceptedAt !== null && cachedUser?.aiConsentAcceptedAt !== undefined) {
            return true;
        }

        const freshUser = await firstValueFrom(this.userFacade.getInfoSilently());
        if (freshUser?.aiConsentAcceptedAt !== null && freshUser?.aiConsentAcceptedAt !== undefined) {
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

        if (accepted !== true) {
            return false;
        }

        await firstValueFrom(this.userFacade.acceptAiConsent());
        return true;
    }

    private stopListening(): void {
        if (this.speechRecognition !== null) {
            this.speechRecognition.stop();
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
        request$: Observable<FoodVisionResponse>,
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
                catchError((err: unknown) => {
                    const status = getNumberProperty(err, 'status');
                    if (status === HttpStatusCode.Forbidden) {
                        state.errorKey.set(errorKeys.premium);
                    } else if (status === HttpStatusCode.TooManyRequests) {
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
                if (response === null) {
                    return;
                }

                const items = response.items;
                state.results.set(items);
                if (items.length > 0) {
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

        this.aiFoodFacade
            .calculateNutrition({ items })
            .pipe(
                catchError((err: unknown) => {
                    const status = getNumberProperty(err, 'status');
                    if (status === HttpStatusCode.TooManyRequests) {
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
                if (response === null) {
                    return;
                }

                state.nutrition.set(response);
            });
    }
}
