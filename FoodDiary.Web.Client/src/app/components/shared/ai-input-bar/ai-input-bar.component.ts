import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { MatIconModule } from '@angular/material/icon';
import { finalize, firstValueFrom } from 'rxjs';
import { catchError, of } from 'rxjs';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { AiFoodService } from '../../../shared/api/ai-food.service';
import { UserService } from '../../../shared/api/user.service';
import { LocalizationService } from '../../../services/localization.service';
import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { FoodNutritionResponse, FoodVisionItem, FoodVisionResponse } from '../../../shared/models/ai.data';
import { ImageSelection } from '../../../shared/models/image-upload.data';
import { PremiumRequiredDialogComponent } from '../premium-required-dialog/premium-required-dialog.component';
import { AiConsentDialogComponent } from '../ai-consent-dialog/ai-consent-dialog.component';
import { AiPhotoResultComponent } from './ai-photo-result/ai-photo-result.component';
import { PhotoUploadDialogComponent } from './photo-upload-dialog/photo-upload-dialog.component';
import { AiInputBarResult, AiRecognitionSource } from './ai-input-bar.types';

@Component({
    selector: 'fd-ai-input-bar',
    templateUrl: './ai-input-bar.component.html',
    styleUrls: ['./ai-input-bar.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, MatIconModule, FdUiButtonComponent, AiPhotoResultComponent],
})
export class AiInputBarComponent {
    private readonly aiFoodService = inject(AiFoodService);
    private readonly userService = inject(UserService);
    private readonly localizationService = inject(LocalizationService);
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly isProcessing = input<boolean>(false);
    public readonly mealRecognized = output<AiInputBarResult>();

    public readonly voiceText = signal('');
    public readonly isVoiceLoading = signal(false);
    public readonly voiceResult = signal<FoodVisionResponse | null>(null);
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

    public readonly isDisabled = computed(
        () => this.isProcessing() || this.isVoiceLoading() || this.photoIsAnalyzing() || this.photoIsNutritionLoading(),
    );

    private speechRecognition: unknown = null;

    public onTextInput(event: Event): void {
        this.voiceText.set((event.target as HTMLInputElement).value);
    }

    public async submitText(): Promise<void> {
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
        this.lastTextSource = 'Text';
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
                this.lastTextSource = 'Voice';
                void this.submitText();
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

    public dismissResult(): void {
        this.voiceResult.set(null);
    }

    public onActionClick(): void {
        const result = this.voiceResult();
        if (!result?.items.length || this.isDisabled()) {
            return;
        }

        this.isVoiceLoading.set(true);
        const text = this.voiceText();
        const source = this.lastTextSource;

        this.aiFoodService
            .calculateNutrition({ items: result.items })
            .pipe(
                finalize(() => this.isVoiceLoading.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: nutrition => {
                    this.mealRecognized.emit({
                        source,
                        recognizedAtUtc: new Date().toISOString(),
                        notes: text,
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
                    });
                },
            });
    }

    public async onPhotoClick(): Promise<void> {
        if (!this.ensurePremium()) {
            return;
        }

        if (!(await this.ensureAiConsent())) {
            return;
        }

        this.dismissResult();

        const selection = await firstValueFrom(
            this.fdDialogService
                .open<PhotoUploadDialogComponent, never, ImageSelection | null>(PhotoUploadDialogComponent, { size: 'lg' })
                .afterClosed(),
        );

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

    public onPhotoAddToMeal(): void {
        const nutrition = this.photoNutrition();
        if (!nutrition) {
            return;
        }

        const selection = this.photoSelection();
        const results = this.photoResults();
        const items =
            nutrition.items?.map(item => {
                const match = results.find(
                    r =>
                        r.nameEn?.trim().toLowerCase() === (item.name ?? '').trim().toLowerCase() ||
                        r.nameLocal?.trim().toLowerCase() === (item.name ?? '').trim().toLowerCase(),
                );
                return {
                    nameEn: match?.nameEn ?? item.name,
                    nameLocal: match?.nameLocal ?? null,
                    amount: item.amount,
                    unit: item.unit,
                    calories: item.calories,
                    proteins: item.protein,
                    fats: item.fat,
                    carbs: item.carbs,
                    fiber: item.fiber,
                    alcohol: item.alcohol,
                };
            }) ?? [];

        this.mealRecognized.emit({
            source: 'Photo',
            imageAssetId: selection?.assetId ?? null,
            imageUrl: selection?.url ?? null,
            recognizedAtUtc: new Date().toISOString(),
            notes: nutrition.notes ?? null,
            items,
        });
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
        this.voiceResult.set(null);
        this.voiceText.set('');
        this.dismissPhotoResult();
    }

    private runPhotoAnalysis(assetId: string): void {
        this.photoIsAnalyzing.set(true);
        this.aiFoodService
            .analyzeFoodImage({ imageAssetId: assetId })
            .pipe(
                catchError(err => {
                    if (err?.status === 403) {
                        this.photoErrorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_PREMIUM');
                    } else if (err?.status === 429) {
                        this.photoErrorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA');
                    } else {
                        this.photoErrorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_GENERIC');
                    }
                    return of(null);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(response => {
                this.photoIsAnalyzing.set(false);
                if (!response) {
                    return;
                }
                const items = response.items ?? [];
                this.photoResults.set(items);
                if (items.length) {
                    this.runPhotoNutrition(items);
                }
            });
    }

    private runPhotoNutrition(items: FoodVisionItem[]): void {
        this.photoIsNutritionLoading.set(true);
        this.photoNutrition.set(null);
        this.photoNutritionErrorKey.set(null);

        this.aiFoodService
            .calculateNutrition({ items })
            .pipe(
                catchError(err => {
                    if (err?.status === 429) {
                        this.photoNutritionErrorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA');
                    } else {
                        this.photoNutritionErrorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.NUTRITION_ERROR');
                    }
                    return of(null);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(response => {
                this.photoIsNutritionLoading.set(false);
                if (!response) {
                    return;
                }
                this.photoNutrition.set(response);
            });
    }

    private ensurePremium(): boolean {
        if (this.authService.isPremium()) {
            return true;
        }

        this.fdDialogService
            .open<PremiumRequiredDialogComponent, never, boolean>(PremiumRequiredDialogComponent, { size: 'sm' })
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

        const freshUser = await firstValueFrom(this.userService.getInfo());
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
}
