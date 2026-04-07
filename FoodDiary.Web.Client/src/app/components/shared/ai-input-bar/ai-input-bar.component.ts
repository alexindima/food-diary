import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { MatIconModule } from '@angular/material/icon';
import { finalize, firstValueFrom } from 'rxjs';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { AiFoodService } from '../../../shared/api/ai-food.service';
import { UserService } from '../../../shared/api/user.service';
import { LocalizationService } from '../../../services/localization.service';
import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { FoodVisionResponse } from '../../../shared/models/ai.data';
import { PremiumRequiredDialogComponent } from '../premium-required-dialog/premium-required-dialog.component';
import { AiConsentDialogComponent } from '../ai-consent-dialog/ai-consent-dialog.component';
import { AiInputBarTextResult } from './ai-input-bar.types';

@Component({
    selector: 'fd-ai-input-bar',
    templateUrl: './ai-input-bar.component.html',
    styleUrls: ['./ai-input-bar.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, MatIconModule, FdUiButtonComponent],
})
export class AiInputBarComponent {
    private readonly aiFoodService = inject(AiFoodService);
    private readonly userService = inject(UserService);
    private readonly localizationService = inject(LocalizationService);
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly placeholder = input<string>();
    public readonly actionLabel = input<string>();
    public readonly isProcessing = input<boolean>(false);

    public readonly textParsed = output<AiInputBarTextResult>();
    public readonly photoRequested = output<void>();

    public readonly voiceText = signal('');
    public readonly isVoiceLoading = signal(false);
    public readonly voiceResult = signal<FoodVisionResponse | null>(null);
    public readonly isListening = signal(false);
    public readonly isSpeechSupported =
        typeof window !== 'undefined' && ('SpeechRecognition' in window || 'webkitSpeechRecognition' in window);

    public readonly isDisabled = computed(() => this.isProcessing() || this.isVoiceLoading());

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
        if (!result) {
            return;
        }

        this.textParsed.emit({ text: this.voiceText(), result });
    }

    public async onPhotoClick(): Promise<void> {
        if (!this.ensurePremium()) {
            return;
        }

        if (!(await this.ensureAiConsent())) {
            return;
        }

        this.photoRequested.emit();
    }

    public clearState(): void {
        this.voiceResult.set(null);
        this.voiceText.set('');
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
