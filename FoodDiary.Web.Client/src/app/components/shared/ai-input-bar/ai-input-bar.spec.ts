import { HttpStatusCode } from '@angular/common/http';
import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { NEVER, type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { AuthService } from '../../../services/auth.service';
import { FrontendLoggerService } from '../../../services/frontend-logger.service';
import { NavigationService } from '../../../services/navigation.service';
import { LocalizationService } from '../../../shared/i18n/localization.service';
import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import { ImageUploadFacade } from '../../../shared/lib/image-upload.facade';
import { UserFacade } from '../../../shared/lib/user.facade';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../shared/models/ai.data';
import type { FoodRecognitionJob } from '../../../shared/models/food-recognition.data';
import { type SpeechRecognitionFailure, SpeechRecognitionService } from '../../../shared/platform/speech-recognition.service';
import { FoodRecognitionHistoryDialogComponent } from '../food-recognition-history/food-recognition-history-dialog';
import { AiInputBarComponent } from './ai-input-bar';
import type { AiInputBarMealDetails, AiInputBarResult } from './ai-input-bar.types';
import { AiPhotoResultComponent } from './ai-photo-result/ai-photo-result';

const RECOGNIZED_AT_PATTERN = /^\d{4}-\d{2}-\d{2}T/;
const VISION_ITEMS: FoodVisionItem[] = [{ nameEn: 'egg', nameLocal: null, amount: 100, unit: 'g', confidence: 1 }];
const NUTRITION: FoodNutritionResponse = {
    calories: 155,
    protein: 12,
    fat: 10,
    carbs: 1,
    fiber: 0,
    alcohol: 0,
    items: [
        {
            name: 'egg',
            amount: 100,
            unit: 'g',
            calories: 155,
            protein: 12,
            fat: 10,
            carbs: 1,
            fiber: 0,
            alcohol: 0,
        },
    ],
};
const MEAL_DETAILS: AiInputBarMealDetails = {
    date: '2026-05-17',
    time: '09:30',
    comment: 'Breakfast',
};
const RECENT_JOB: FoodRecognitionJob = {
    id: 'job-1',
    imageAssetId: 'asset-1',
    imageUrl: 'https://example.com/photo.jpg',
    description: null,
    status: 'Succeeded',
    createdOnUtc: '2026-05-17T00:00:00Z',
    updatedOnUtc: '2026-05-17T00:00:00Z',
    vision: { items: VISION_ITEMS },
    nutrition: NUTRITION,
    errorCode: null,
    nutritionErrorCode: null,
};

type AiInputBarTestContext = {
    aiFoodService: {
        analyzeFoodImage: ReturnType<typeof vi.fn>;
        calculateNutrition: ReturnType<typeof vi.fn>;
        parseFoodText: ReturnType<typeof vi.fn>;
        resumeRecognition: ReturnType<typeof vi.fn>;
    };
    component: AiInputBarComponent;
    dialogService: {
        open: ReturnType<typeof vi.fn>;
    };
    fixture: ComponentFixture<AiInputBarComponent>;
    navigationService: {
        navigateToPremiumAccessAsync: ReturnType<typeof vi.fn>;
    };
    userFacade: {
        acceptAiConsent: ReturnType<typeof vi.fn>;
        getInfoSilently: ReturnType<typeof vi.fn>;
        user: ReturnType<typeof signal<{ aiConsentAcceptedAt: string | null } | null>>;
    };
    speechRecognition: SpeechRecognitionMock;
};

type SpeechRecognitionMock = {
    isSupported: boolean;
    isListening: ReturnType<typeof signal<boolean>>;
    start: Mock<SpeechRecognitionService['start']>;
    stop: Mock<SpeechRecognitionService['stop']>;
    isOwnedBy: SpeechRecognitionService['isOwnedBy'];
};

async function setupAiInputBarAsync(
    mode: 'create' | 'emit' = 'emit',
    options: { aiConsentAcceptedAt?: string | null; isPremium?: boolean; speechSupported?: boolean } = {},
): Promise<AiInputBarTestContext> {
    const aiFoodService = {
        parseFoodText: vi.fn().mockReturnValue(of({ items: VISION_ITEMS })),
        analyzeFoodImage: vi.fn().mockReturnValue(of({ items: VISION_ITEMS })),
        calculateNutrition: vi.fn().mockReturnValue(of(NUTRITION)),
        resumeRecognition: vi
            .fn()
            .mockReturnValue(of({ items: VISION_ITEMS, recognition: { id: RECENT_JOB.id, nutrition: NUTRITION, errorCode: null } })),
    };
    const aiConsentAcceptedAt: string | null =
        options.aiConsentAcceptedAt === undefined ? '2026-05-17T00:00:00Z' : options.aiConsentAcceptedAt;
    const userFacade = {
        user: signal({ aiConsentAcceptedAt }),
        getInfoSilently: vi.fn().mockReturnValue(of(null)),
        acceptAiConsent: vi.fn().mockReturnValue(of(void 0)),
    };
    const navigationService = { navigateToPremiumAccessAsync: vi.fn() };
    const speechRecognition = createSpeechRecognitionMock(options.speechSupported ?? false);
    const dialogService = {
        open: vi.fn(
            (
                component: unknown,
            ): {
                afterClosed: () => Observable<boolean | FoodRecognitionJob | undefined>;
                componentInstance?: null;
                componentRef?: null;
                close?: () => void;
            } =>
                component === AiPhotoResultComponent || component === FoodRecognitionHistoryDialogComponent
                    ? { componentInstance: null, componentRef: null, close: vi.fn(), afterClosed: () => NEVER }
                    : { afterClosed: () => of(true) },
        ),
    };

    await TestBed.configureTestingModule({
        imports: [AiInputBarComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AiFoodFacade, useValue: aiFoodService },
            {
                provide: UserFacade,
                useValue: userFacade,
            },
            { provide: AuthService, useValue: { isPremium: signal(options.isPremium ?? true) } },
            { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } },
            { provide: NavigationService, useValue: navigationService },
            { provide: FdUiDialogService, useValue: dialogService },
            {
                provide: ImageUploadFacade,
                useValue: {
                    requestUploadUrl: vi.fn(),
                    uploadToPresignedUrl: vi.fn(),
                    deleteAsset: vi.fn().mockReturnValue(of(void 0)),
                },
            },
            { provide: FrontendLoggerService, useValue: { warn: vi.fn() } },
            { provide: SpeechRecognitionService, useValue: speechRecognition },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiInputBarComponent);
    const component = fixture.componentInstance;
    fixture.componentRef.setInput('mode', mode);
    return { aiFoodService, component, dialogService, fixture, navigationService, userFacade, speechRecognition };
}

function createSpeechRecognitionMock(isSupported: boolean): SpeechRecognitionMock {
    const isListening = signal(false);
    let activeOwner: object | undefined;
    return {
        isSupported,
        isListening,
        start: vi.fn<SpeechRecognitionService['start']>((_locale, _onTranscript, _onError, owner) => {
            activeOwner = owner;
            isListening.set(true);
            return true;
        }),
        stop: vi.fn<SpeechRecognitionService['stop']>(owner => {
            if (owner === undefined || activeOwner === owner) {
                isListening.set(false);
            }
        }),
        isOwnedBy: owner => activeOwner === owner,
    };
}

function reportSpeechFailure(speech: SpeechRecognitionMock, failure: SpeechRecognitionFailure): void {
    speech.isListening.set(false);
    const onError = speech.start.mock.calls.at(-1)?.[2];
    if (onError === undefined) {
        throw new Error('Speech error callback was not registered');
    }
    onError(failure);
}

const SPEECH_FAILURE_CASES: Array<[SpeechRecognitionFailure, string]> = [
    ['microphone-unavailable', 'AI_INPUT_BAR.VOICE_ERROR_MICROPHONE'],
    ['permission-denied', 'AI_INPUT_BAR.VOICE_ERROR_PERMISSION'],
    ['network', 'AI_INPUT_BAR.VOICE_ERROR_NETWORK'],
    ['failed', 'AI_INPUT_BAR.VOICE_ERROR_GENERIC'],
];

describe('AiInputBarComponent speech feedback', () => {
    it.each(SPEECH_FAILURE_CASES)('announces a safe %s error without submitting a meal', async (failure, key) => {
        const { component, fixture, speechRecognition, aiFoodService } = await setupAiInputBarAsync('emit', { speechSupported: true });
        fixture.detectChanges();
        await component['toggleMicAsync']();

        reportSpeechFailure(speechRecognition, failure);
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain(key);
        expect(component['isListening']()).toBe(false);
        expect(aiFoodService.parseFoodText).not.toHaveBeenCalled();
    });

    it('explains unsupported voice input and retains the text alternative', async () => {
        const { component, fixture, speechRecognition } = await setupAiInputBarAsync();
        fixture.detectChanges();

        await component['toggleMicAsync']();
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('AI_INPUT_BAR.VOICE_ERROR_UNSUPPORTED');
        expect(host.querySelector('.ai-input-bar__input')).not.toBeNull();
        expect(speechRecognition.start).not.toHaveBeenCalled();
    });

    it('clears a failure on retry and stops normally without an alert', async () => {
        const { component, fixture, speechRecognition } = await setupAiInputBarAsync('emit', { speechSupported: true });
        fixture.detectChanges();
        await component['toggleMicAsync']();
        reportSpeechFailure(speechRecognition, 'network');
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).not.toBeNull();

        await component['toggleMicAsync']();
        fixture.detectChanges();

        expect(component['isListening']()).toBe(true);
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();
        await component['toggleMicAsync']();
        fixture.detectChanges();
        expect(component['isListening']()).toBe(false);
        expect(speechRecognition.stop).toHaveBeenCalledOnce();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();
    });
});

describe('AiInputBarComponent speech recovery', () => {
    it('allows manual text after a failure and clears the obsolete speech alert', async () => {
        const { component, fixture, speechRecognition, aiFoodService } = await setupAiInputBarAsync('emit', { speechSupported: true });
        fixture.detectChanges();
        await component['toggleMicAsync']();
        reportSpeechFailure(speechRecognition, 'permission-denied');
        fixture.detectChanges();
        const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('.ai-input-bar__input');
        if (input === null) {
            throw new Error('Meal text input was not rendered');
        }

        input.value = 'two eggs';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();
        expect(component['voiceText']()).toBe('two eggs');
        await component['submitTextAsync']();
        expect(aiFoodService.parseFoodText).toHaveBeenCalledWith({ text: 'two eggs' });
    });

    it('clears local speech failure when the parent resets the input', async () => {
        const { component, fixture, speechRecognition } = await setupAiInputBarAsync('create', { speechSupported: true });
        fixture.detectChanges();
        await component['toggleMicAsync']();
        reportSpeechFailure(speechRecognition, 'failed');
        fixture.detectChanges();

        fixture.componentRef.setInput('clearToken', 1);
        fixture.detectChanges();

        expect(component['speechErrorKey']()).toBeNull();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();
    });

    it('still submits a successful transcript as voice recognition', async () => {
        const { component, fixture, speechRecognition, aiFoodService } = await setupAiInputBarAsync('emit', { speechSupported: true });
        fixture.detectChanges();
        await component['toggleMicAsync']();
        const onTranscript = speechRecognition.start.mock.calls[0][1];

        onTranscript('two eggs');
        await fixture.whenStable();

        expect(aiFoodService.parseFoodText).toHaveBeenCalledWith({ text: 'two eggs' });
        expect(component['lastTextSource']).toBe('Voice');
        expect(component['speechErrorKey']()).toBeNull();
    });
});

describe('AiInputBarComponent speech ownership', () => {
    it('disables only another microphone and preserves the owner recording through unrelated cleanup', async () => {
        const { component, fixture, speechRecognition } = await setupAiInputBarAsync('emit', { speechSupported: true });
        const otherInput = TestBed.createComponent(AiInputBarComponent);
        fixture.detectChanges();
        otherInput.detectChanges();
        await component['toggleMicAsync']();
        const owner = speechRecognition.start.mock.calls[0][3];
        otherInput.detectChanges();
        const otherHost = otherInput.nativeElement as HTMLElement;
        const microphone = otherHost.querySelector<HTMLButtonElement>('button[aria-label="MEAL_LIST.VOICE_MIC_TITLE"]');
        expect(microphone?.disabled).toBe(true);
        expect(microphone?.getAttribute('title')).toBe('DISABLED_HINTS.OPERATION_BUSY');
        expect(otherHost.querySelector<HTMLInputElement>('.ai-input-bar__input')?.disabled).toBe(false);
        expect(otherHost.querySelector<HTMLButtonElement>('button[aria-label="AI_INPUT_BAR.PHOTO_TITLE"]')?.disabled).toBe(false);
        await otherInput.componentInstance['toggleMicAsync']();
        expect(speechRecognition.start).toHaveBeenCalledOnce();

        otherInput.destroy();

        expect(speechRecognition.isListening()).toBe(true);
        expect(component['isListening']()).toBe(true);
        fixture.destroy();
        expect(speechRecognition.isListening()).toBe(false);
        expect(speechRecognition.stop).toHaveBeenLastCalledWith(owner);
    });

    it('does not start recording after a pending consent check outlives the input', async () => {
        const { component, fixture, speechRecognition, userFacade } = await setupAiInputBarAsync('emit', {
            speechSupported: true,
            aiConsentAcceptedAt: null,
        });
        const user = new Subject<{ aiConsentAcceptedAt: string }>();
        userFacade.getInfoSilently.mockReturnValueOnce(user);
        fixture.detectChanges();
        const recording = component['toggleMicAsync']();

        fixture.destroy();
        user.next({ aiConsentAcceptedAt: '2026-05-17T00:00:00Z' });
        await recording;

        expect(speechRecognition.start).not.toHaveBeenCalled();
    });
});

describe('AiInputBarComponent history access', () => {
    it('opens recent recognitions from the food input', async () => {
        const { dialogService, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();
        const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button'));
        const historyButton = buttons.find(button => button.textContent.includes('AI_RECOGNITION.SHOW_RECENT'));

        expect(historyButton).toBeDefined();
        historyButton?.click();

        expect(dialogService.open).toHaveBeenCalledOnce();
    });
});

describe('AiInputBarComponent text recognition', () => {
    it('provides an accessible name for the text input', async () => {
        const { fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();

        const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('.ai-input-bar__input');

        expect(input?.getAttribute('aria-label')).toBeTruthy();
    });

    it('runs text recognition and nutrition calculation', async () => {
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync();
        component['voiceText'].set(' eggs ');
        fixture.detectChanges();

        await component['submitTextAsync']();

        expect(aiFoodService.parseFoodText).toHaveBeenCalledWith({ text: 'eggs' });
        expect(aiFoodService.calculateNutrition).toHaveBeenCalledWith({ items: VISION_ITEMS });
        expect(component['textSubmittedQuery']()).toBe('eggs');
        expect(component['textNutrition']()).toEqual(NUTRITION);
    });

    it('emits recognized meal in emit mode and clears state', async () => {
        const { component, fixture } = await setupAiInputBarAsync('emit');
        const recognizedSpy = vi.fn<(result: AiInputBarResult) => void>();
        component['mealRecognized'].subscribe(result => {
            recognizedSpy(result);
        });
        component['textSubmittedQuery'].set('eggs');
        component['textResults'].set(VISION_ITEMS);
        component['textNutrition'].set(NUTRITION);
        fixture.detectChanges();

        component['onTextAddToMeal'](MEAL_DETAILS);

        expect(recognizedSpy).toHaveBeenCalledOnce();
        const recognizedResult = recognizedSpy.mock.calls[0][0];
        expect(recognizedResult.source).toBe('Text');
        expect(recognizedResult.notes).toBe('eggs');
        expect(recognizedResult.date).toBe('2026-05-17');
        expect(recognizedResult.time).toBe('09:30');
        expect(recognizedResult.recognizedAtUtc).toMatch(RECOGNIZED_AT_PATTERN);
        expect(component['voiceText']()).toBe('');
        expect(component['hasTextResult']()).toBe(false);
    });

    it('emits created meal in create mode without clearing until parent confirms success', async () => {
        const { component, fixture } = await setupAiInputBarAsync('create');
        const createSpy = vi.fn<(result: AiInputBarResult) => void>();
        component['mealCreateRequested'].subscribe(result => {
            createSpy(result);
        });
        component['textSubmittedQuery'].set('eggs');
        component['textResults'].set(VISION_ITEMS);
        component['textNutrition'].set(NUTRITION);
        fixture.detectChanges();

        component['onTextAddToMeal'](MEAL_DETAILS);

        expect(createSpy).toHaveBeenCalledOnce();
        expect(component['hasTextResult']()).toBe(true);
    });

    it('clears create mode state when clear token changes', async () => {
        const { component, fixture } = await setupAiInputBarAsync('create');
        component['textSubmittedQuery'].set('eggs');
        component['textResults'].set(VISION_ITEMS);
        component['textNutrition'].set(NUTRITION);
        fixture.detectChanges();

        fixture.componentRef.setInput('clearToken', 1);
        fixture.detectChanges();

        expect(component['voiceText']()).toBe('');
        expect(component['hasTextResult']()).toBe(false);
    });

    it('does not request nutrition for empty edited items', async () => {
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync('create');
        component['textResults'].set(VISION_ITEMS);
        component['textNutrition'].set(NUTRITION);
        fixture.detectChanges();

        component['onTextEditApplied']({ items: [], nutrition: null });

        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
        expect(component['textErrorKey']()).toBe('AI_INPUT_BAR.EMPTY_ITEMS_ERROR');
    });
});

describe('AiInputBarComponent access gates and errors', () => {
    it('opens premium dialog and navigates when non-premium user confirms upgrade', async () => {
        const { aiFoodService, component, dialogService, fixture, navigationService } = await setupAiInputBarAsync('emit', {
            isPremium: false,
        });
        component['voiceText'].set('eggs');
        fixture.detectChanges();

        await component['submitTextAsync']();

        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(navigationService.navigateToPremiumAccessAsync).toHaveBeenCalledOnce();
        expect(aiFoodService.parseFoodText).not.toHaveBeenCalled();
    });

    it('asks for AI consent when cached and fresh user do not have it', async () => {
        const { aiFoodService, component, dialogService, fixture, userFacade } = await setupAiInputBarAsync('emit', {
            aiConsentAcceptedAt: null,
        });
        component['voiceText'].set('eggs');
        fixture.detectChanges();

        await component['submitTextAsync']();

        expect(userFacade.getInfoSilently).toHaveBeenCalledOnce();
        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(userFacade.acceptAiConsent).toHaveBeenCalledOnce();
        expect(aiFoodService.parseFoodText).toHaveBeenCalledWith({ text: 'eggs' });
    });

    it('maps text analysis quota errors to text error key', async () => {
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync();
        aiFoodService.parseFoodText.mockReturnValueOnce(throwError(() => ({ status: HttpStatusCode.TooManyRequests })));
        component['voiceText'].set('eggs');
        fixture.detectChanges();

        await component['submitTextAsync']();

        expect(component['textErrorKey']()).toBe('AI_INPUT_BAR.TEXT_ERROR_QUOTA');
        expect(component['textIsAnalyzing']()).toBe(false);
        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
    });
});

describe('AiInputBarComponent recognition history', () => {
    it('resumes stored nutrition without premium, new consent, or another AI request', async () => {
        const { aiFoodService, component, dialogService, fixture, userFacade } = await setupAiInputBarAsync('create', {
            isPremium: false,
            aiConsentAcceptedAt: null,
        });
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(RECENT_JOB) });
        fixture.detectChanges();
        const createSpy = vi.fn();
        component.mealCreateRequested.subscribe(createSpy);

        component['openRecognitionHistory']();

        expect(dialogService.open).toHaveBeenNthCalledWith(1, FoodRecognitionHistoryDialogComponent, { preset: 'list', size: 'lg' });
        expect(aiFoodService.resumeRecognition).toHaveBeenCalledWith(RECENT_JOB.id);
        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
        expect(userFacade.getInfoSilently).not.toHaveBeenCalled();
        expect(userFacade.acceptAiConsent).not.toHaveBeenCalled();
        expect(component['photoSelection']()).toEqual({ url: RECENT_JOB.imageUrl, assetId: RECENT_JOB.imageAssetId });
        expect(component['photoNutrition']()).toEqual(NUTRITION);
        expect(createSpy).not.toHaveBeenCalled();

        component['onPhotoAddToMeal'](MEAL_DETAILS);

        expect(createSpy).toHaveBeenCalledWith(expect.objectContaining({ source: 'Photo', imageAssetId: RECENT_JOB.imageAssetId }));
    });

    it('preserves the unsubmitted text when history is dismissed', async () => {
        const { aiFoodService, component, dialogService, fixture } = await setupAiInputBarAsync();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(undefined) });
        component['voiceText'].set('unfinished breakfast');
        fixture.detectChanges();

        component['openRecognitionHistory']();

        expect(component['voiceText']()).toBe('unfinished breakfast');
        expect(aiFoodService.resumeRecognition).not.toHaveBeenCalled();
    });

    it('prevents duplicate dialogs and ignores pending history after component destruction', async () => {
        const { aiFoodService, component, dialogService, fixture } = await setupAiInputBarAsync();
        const selected = new Subject<FoodRecognitionJob>();
        const close = vi.fn();
        dialogService.open.mockReturnValueOnce({ close, afterClosed: () => selected });
        fixture.detectChanges();
        component['openRecognitionHistory']();
        component['openRecognitionHistory']();

        expect(dialogService.open).toHaveBeenCalledOnce();
        fixture.destroy();
        selected.next(RECENT_JOB);

        expect(close).toHaveBeenCalledOnce();
        expect(selected.observed).toBe(false);
        expect(aiFoodService.resumeRecognition).not.toHaveBeenCalled();
    });

    it('does not open history while recognition is already processing', async () => {
        const { component, dialogService, fixture } = await setupAiInputBarAsync();
        fixture.componentRef.setInput('isProcessing', true);
        fixture.detectChanges();

        component['openRecognitionHistory']();

        expect(dialogService.open).not.toHaveBeenCalled();
    });

    it('does not resume a product label as a meal photo', async () => {
        const { aiFoodService, component, dialogService, fixture } = await setupAiInputBarAsync();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of({ ...RECENT_JOB, isProductLabel: true }) });
        fixture.detectChanges();

        component['openRecognitionHistory']();

        expect(aiFoodService.resumeRecognition).not.toHaveBeenCalled();
    });
});

describe('AiInputBarComponent saved photo reanalysis', () => {
    it('requires premium for a new analysis of a stored photo', async () => {
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync('create', { isPremium: false });
        component['photoSelection'].set({ url: RECENT_JOB.imageUrl, assetId: RECENT_JOB.imageAssetId });
        fixture.detectChanges();

        await component['onPhotoReanalyzeAsync']();

        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
    });

    it('preserves the saved result when consent for another analysis is declined', async () => {
        const { aiFoodService, component, dialogService, fixture, userFacade } = await setupAiInputBarAsync('create', {
            aiConsentAcceptedAt: null,
        });
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(false) });
        component['photoSelection'].set({ url: RECENT_JOB.imageUrl, assetId: RECENT_JOB.imageAssetId });
        component['photoNutrition'].set(NUTRITION);
        fixture.detectChanges();

        await component['onPhotoReanalyzeAsync']();

        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
        expect(userFacade.acceptAiConsent).not.toHaveBeenCalled();
        expect(component['photoNutrition']()).toBe(NUTRITION);
    });

    it('does not start a new analysis if the photo was dismissed during consent', async () => {
        const { aiFoodService, component, fixture, userFacade } = await setupAiInputBarAsync('create', {
            aiConsentAcceptedAt: null,
        });
        const user = new Subject<{ aiConsentAcceptedAt: string }>();
        userFacade.getInfoSilently.mockReturnValueOnce(user);
        component['photoSelection'].set({ url: RECENT_JOB.imageUrl, assetId: RECENT_JOB.imageAssetId });
        fixture.detectChanges();
        const reanalysis = component['onPhotoReanalyzeAsync']();
        component['dismissPhotoResult']();
        user.next({ aiConsentAcceptedAt: '2026-05-17T00:00:00Z' });
        await reanalysis;

        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
    });
});

describe('AiInputBarComponent repeated photo recognition', () => {
    it('keeps the next photo modal open after a successful create clears the previous result', async () => {
        const { component, dialogService, fixture } = await setupAiInputBarAsync('create');
        fixture.detectChanges();

        fixture.componentRef.setInput('clearToken', 1);
        fixture.detectChanges();

        component['onPhotoPreparationStarted']('blob:second-photo');
        fixture.detectChanges();

        expect(dialogService.open).toHaveBeenCalledWith(
            AiPhotoResultComponent,
            expect.objectContaining({ panelClass: 'fd-ai-photo-result-dialog-panel' }),
        );
        expect(component['photoSelection']()).toEqual({ url: 'blob:second-photo', assetId: null });
        expect(component['photoDialogRef']()).not.toBeNull();
    });
});

describe('AiInputBarComponent photo recognition', () => {
    it('opens the modal immediately with the local preview while the image is being prepared', async () => {
        const { aiFoodService, component, dialogService, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();

        component['onPhotoPreparationStarted']('blob:local-preview');
        fixture.detectChanges();

        expect(dialogService.open).toHaveBeenCalledWith(
            AiPhotoResultComponent,
            expect.objectContaining({ size: 'xl', panelClass: 'fd-ai-photo-result-dialog-panel' }),
        );
        expect(component['photoSelection']()).toEqual({ url: 'blob:local-preview', assetId: null });
        expect(component['photoIsPreparing']()).toBe(true);
        expect(component['isDisabled']()).toBe(true);
        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
    });

    it('ignores photo selections without asset id', async () => {
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();

        component['onPhotoSelected']({ url: 'https://example.com/photo.jpg', assetId: null });

        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
        expect(component['hasPhotoResult']()).toBe(false);
    });

    it('runs photo recognition when asset id is present', async () => {
        const { aiFoodService, component, dialogService, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();

        component['onPhotoSelected']({ url: 'https://example.com/photo.jpg', assetId: 'asset-1' });

        expect(dialogService.open).toHaveBeenCalledWith(
            AiPhotoResultComponent,
            expect.objectContaining({ size: 'xl', panelClass: 'fd-ai-photo-result-dialog-panel' }),
        );
        expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledWith({ imageAssetId: 'asset-1' });
        expect(component['photoIsPreparing']()).toBe(false);
        expect(aiFoodService.calculateNutrition).toHaveBeenCalledWith({ items: VISION_ITEMS });
        expect(component['photoNutrition']()).toEqual(NUTRITION);
    });

    it('keeps the preparation modal open and replaces its preview URL after upload', async () => {
        const { component, dialogService, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();

        component['onPhotoPreparationStarted']('blob:local-preview');
        const dialogConfig = dialogService.open.mock.calls[0][1] as {
            data: { imageUrl: () => string | null; isPreparing: () => boolean };
        };

        expect(dialogConfig.data.imageUrl()).toBe('blob:local-preview');
        expect(dialogConfig.data.isPreparing()).toBe(true);

        component['onPhotoSelected']({ url: 'https://cdn.example.com/photo.jpg', assetId: 'asset-1' });

        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(component['photoSelection']()).toEqual({ url: 'https://cdn.example.com/photo.jpg', assetId: 'asset-1' });
        expect(dialogConfig.data.imageUrl()).toBe('https://cdn.example.com/photo.jpg');
        expect(dialogConfig.data.isPreparing()).toBe(false);
    });

    it('emits recognized photo meal with image metadata', async () => {
        const { component, fixture } = await setupAiInputBarAsync();
        const recognizedSpy = vi.fn<(result: AiInputBarResult) => void>();
        component['mealRecognized'].subscribe(result => {
            recognizedSpy(result);
        });
        component['photoSelection'].set({ url: 'https://example.com/photo.jpg', assetId: 'asset-1' });
        component['photoResults'].set(VISION_ITEMS);
        component['photoNutrition'].set(NUTRITION);
        fixture.detectChanges();

        component['onPhotoAddToMeal'](MEAL_DETAILS);

        expect(recognizedSpy).toHaveBeenCalledOnce();
        const result = recognizedSpy.mock.calls[0][0];
        expect(result.source).toBe('Photo');
        expect(result.imageAssetId).toBe('asset-1');
        expect(result.imageUrl).toBe('https://example.com/photo.jpg');
        expect(component['hasPhotoResult']()).toBe(false);
    });

    it('maps photo nutrition failures to nutrition error key', async () => {
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync();
        aiFoodService.calculateNutrition.mockReturnValueOnce(throwError(() => ({ status: HttpStatusCode.TooManyRequests })));
        fixture.detectChanges();

        component['onPhotoSelected']({ url: 'https://example.com/photo.jpg', assetId: 'asset-1' });

        expect(component['photoNutritionErrorKey']()).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA');
        expect(component['photoIsNutritionLoading']()).toBe(false);
    });
});
