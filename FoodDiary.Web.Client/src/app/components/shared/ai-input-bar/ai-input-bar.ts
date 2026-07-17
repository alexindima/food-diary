import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { LocalizationService } from '../../../shared/i18n/localization.service';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import { UserFacade } from '../../../shared/lib/user.facade';
import type { ImageSelection } from '../../../shared/models/image-upload.data';
import { SpeechRecognitionService } from '../../../shared/platform/speech-recognition.service';
import { AiConsentDialogComponent } from '../ai-consent-dialog/ai-consent-dialog';
import { ImageUploadFieldComponent } from '../image-upload-field/image-upload-field';
import { PremiumRequiredDialogComponent } from '../premium-required-dialog/premium-required-dialog';
import { AiInputBarFacade } from './ai-input-bar.facade';
import { buildPhotoAiInputBarResult, buildTextAiInputBarResult } from './ai-input-bar.mapper';
import type { AiInputBarMealDetails, AiInputBarMode, AiInputBarResult, AiRecognitionSource } from './ai-input-bar.types';
import { AiPhotoResultComponent } from './ai-photo-result/ai-photo-result';
import type { AiPhotoEditApplied } from './ai-photo-result/ai-photo-result-lib/ai-photo-result.types';

@Component({
    selector: 'fd-ai-input-bar',
    templateUrl: './ai-input-bar.html',
    styleUrls: ['./ai-input-bar.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent, AiPhotoResultComponent, ImageUploadFieldComponent],
    providers: [AiInputBarFacade],
})
export class AiInputBarComponent {
    private readonly recognition = inject(AiInputBarFacade);
    private readonly userFacade = inject(UserFacade);
    private readonly localizationService = inject(LocalizationService);
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly speechRecognition = inject(SpeechRecognitionService);
    private readonly photoUploadField = viewChild(ImageUploadFieldComponent);

    public readonly isProcessing = input<boolean>(false);
    public readonly clearToken = input(0);
    public readonly mode = input<AiInputBarMode>('emit');
    public readonly mealType = input<string | null>(null);
    public readonly mealRecognized = output<AiInputBarResult>();
    public readonly mealCreateRequested = output<AiInputBarResult>();

    protected readonly voiceText = signal('');
    protected readonly textSubmittedQuery = signal<string | null>(null);
    protected readonly textIsAnalyzing = this.recognition.text.analyzing;
    protected readonly textResults = this.recognition.text.results;
    protected readonly textIsNutritionLoading = this.recognition.text.nutritionLoading;
    protected readonly textNutrition = this.recognition.text.nutrition;
    protected readonly textErrorKey = this.recognition.text.errorKey;
    protected readonly textNutritionErrorKey = this.recognition.text.nutritionErrorKey;
    protected readonly hasTextResult = computed(() => this.textSubmittedQuery() !== null);
    protected readonly isSubmittingMeal = signal(false);
    protected readonly isListening = this.speechRecognition.isListening;
    protected readonly isSpeechSupported = this.speechRecognition.isSupported;
    private lastTextSource: AiRecognitionSource = 'Text';

    protected readonly photoSelection = signal<ImageSelection | null>(null);
    protected readonly photoIsAnalyzing = this.recognition.photo.analyzing;
    protected readonly photoResults = this.recognition.photo.results;
    protected readonly photoIsNutritionLoading = this.recognition.photo.nutritionLoading;
    protected readonly photoNutrition = this.recognition.photo.nutrition;
    protected readonly photoErrorKey = this.recognition.photo.errorKey;
    protected readonly photoNutritionErrorKey = this.recognition.photo.nutritionErrorKey;
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
            this.speechRecognition.stop();
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

        this.speechRecognition.start(resolveAppLocale(this.localizationService.getCurrentLanguage()), transcript => {
            if (transcript.length > 0) {
                this.voiceText.set(transcript);
                void this.submitTextAsync('Voice');
            }
        });
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
        this.recognition.clear(this.recognition.text);
    }

    protected onTextEditApplied(result: AiPhotoEditApplied): void {
        this.recognition.setEditResult(this.recognition.text, result.items, result.nutrition);
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
        this.recognition.clear(this.recognition.photo);
    }

    protected onPhotoEditApplied(result: AiPhotoEditApplied): void {
        this.recognition.setEditResult(this.recognition.photo, result.items, result.nutrition);
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
        this.recognition.analyzeText(text);
    }

    private runPhotoAnalysis(assetId: string): void {
        this.recognition.analyzePhoto(assetId);
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
}
