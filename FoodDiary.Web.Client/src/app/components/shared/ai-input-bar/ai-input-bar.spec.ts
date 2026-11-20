import { HttpStatusCode } from '@angular/common/http';
import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom, NEVER, type Observable, of, Subject, throwError } from 'rxjs';
import { afterEach, describe, expect, it, type Mock, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { AuthService } from '../../../services/auth.service';
import { FrontendLoggerService } from '../../../services/frontend-logger.service';
import { NavigationService } from '../../../services/navigation.service';
import { LocalizationService } from '../../../shared/i18n/localization.service';
import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import { ImageUploadFacade } from '../../../shared/lib/image-upload.facade';
import { UserFacade } from '../../../shared/lib/user.facade';
import type { FoodNutritionResponse, FoodVisionItem, FoodVisionResponse } from '../../../shared/models/ai.data';
import type { FoodRecognitionJob } from '../../../shared/models/food-recognition.data';
import { type ImageSelection, imageSelection } from '../../../shared/models/image-upload.data';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { publicImageUrl } from '../../../shared/models/semantics/image-location';
import { type SpeechRecognitionFailure, SpeechRecognitionService } from '../../../shared/platform/speech-recognition.service';
import { PrivacyConsentFacade } from '../../../shared/privacy/privacy-consent.facade';
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
    id: entityId<'food-recognition'>('job-1'),
    imageAssetId: entityId<'image-asset'>('asset-1'),
    imageUrl: publicImageUrl('https://example.com/photo.jpg'),
    description: null,
    status: 'Succeeded',
    createdOnUtc: utcInstant('2026-05-17T00:00:00Z'),
    updatedOnUtc: utcInstant('2026-05-17T00:00:00Z'),
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
    options: {
        aiConsentAcceptedAt?: string | null;
        isPremium?: boolean;
        speechSupported?: boolean;
        overlayUpload?: Observable<ImageSelection>;
    } = {},
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
            { provide: PrivacyConsentFacade, useValue: { ensureHealthConsentAsync: vi.fn().mockResolvedValue(true) } },
            {
                provide: UserFacade,
                useValue: userFacade,
            },
            { provide: AuthService, useValue: { isPremium: signal(options.isPremium ?? true), getUserId: (): string => 'ai-draft-user' } },
            { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } },
            { provide: NavigationService, useValue: navigationService },
            ...(options.overlayUpload === undefined ? [{ provide: FdUiDialogService, useValue: dialogService }] : []),
            {
                provide: ImageUploadFacade,
                useValue: {
                    requestUploadUrl: vi.fn(),
                    uploadToPresignedUrl: vi.fn(),
                    upload: vi.fn().mockReturnValue(options.overlayUpload ?? NEVER),
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
    it('stops recording when its composer is collapsed while preserving the text draft', async () => {
        const { component, fixture, speechRecognition } = await setupAiInputBarAsync('create', { speechSupported: true });
        fixture.detectChanges();
        component['voiceText'].set('Dinner draft');
        await component['toggleMicAsync']();
        expect(speechRecognition.isListening()).toBe(true);

        fixture.componentRef.setInput('active', false);
        fixture.detectChanges();

        expect(speechRecognition.isListening()).toBe(false);
        expect(component['voiceText']()).toBe('Dinner draft');
    });

    it('does not start recording if a composer is collapsed while consent is pending', async () => {
        const { component, fixture, speechRecognition, userFacade } = await setupAiInputBarAsync('create', {
            speechSupported: true,
            aiConsentAcceptedAt: null,
        });
        const user = new Subject<{ aiConsentAcceptedAt: string }>();
        userFacade.getInfoSilently.mockReturnValueOnce(user);
        fixture.detectChanges();
        const recording = component['toggleMicAsync']();
        await vi.waitUntil(() => userFacade.getInfoSilently.mock.calls.length === 1);
        fixture.componentRef.setInput('active', false);
        fixture.detectChanges();
        user.next({ aiConsentAcceptedAt: '2026-05-17T00:00:00Z' });
        await recording;

        expect(speechRecognition.start).not.toHaveBeenCalled();
    });

    it('restores typed text after the Premium action destroys and remounts the input', async () => {
        const { component, fixture, navigationService, aiFoodService } = await setupAiInputBarAsync('create', { isPremium: false });
        component['voiceText'].set('  Два яйца и чай  ');
        await component['submitTextAsync']();
        expect(navigationService.navigateToPremiumAccessAsync).toHaveBeenCalledOnce();
        expect(aiFoodService.parseFoodText).not.toHaveBeenCalled();
        fixture.destroy();
        const returned = TestBed.createComponent(AiInputBarComponent);
        expect(returned.componentInstance['voiceText']()).toBe('  Два яйца и чай  ');
    });

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
        await vi.waitUntil(() => userFacade.getInfoSilently.mock.calls.length === 1);

        fixture.destroy();
        user.next({ aiConsentAcceptedAt: '2026-05-17T00:00:00Z' });
        await recording;

        expect(speechRecognition.start).not.toHaveBeenCalled();
    });
});

describe('Rendered AI composer entry points', () => {
    it('leaves another composer’s microphone active when the first consent check returns late', async () => {
        const { fixture, speechRecognition } = await setupAiInputBarAsync('emit', { speechSupported: true });
        const firstConsent = new Subject<boolean>();
        const checkConsent = vi
            .spyOn(TestBed.inject(PrivacyConsentFacade), 'ensureHealthConsentAsync')
            .mockReturnValueOnce(firstValueFrom(firstConsent));
        const other = TestBed.createComponent(AiInputBarComponent);
        fixture.detectChanges();
        other.detectChanges();
        const selector = 'button[aria-label="MEAL_LIST.VOICE_MIC_TITLE"]';
        const firstMic = selectRecognitionControl(fixture, selector, HTMLButtonElement);
        const otherMic = selectRecognitionControl(other, selector, HTMLButtonElement);

        firstMic.click();
        expect(checkConsent).toHaveBeenCalledOnce();
        otherMic.click();
        await vi.waitFor(() => {
            expect(speechRecognition.start).toHaveBeenCalledOnce();
        });

        firstConsent.next(true);
        firstConsent.complete();
        await fixture.whenStable();
        fixture.detectChanges();
        other.detectChanges();

        expect(speechRecognition.start).toHaveBeenCalledOnce();
        expect(firstMic.disabled).toBe(true);
        expect(otherMic.getAttribute('aria-pressed')).toBe('true');
    });

    it('ignores blank input on Enter and recognizes only the next nonempty description', async () => {
        const { fixture, aiFoodService } = await setupAiInputBarAsync();
        const checkConsent = vi.spyOn(TestBed.inject(PrivacyConsentFacade), 'ensureHealthConsentAsync');
        fixture.detectChanges();
        enterRecognitionText(fixture, '   ');
        await fixture.whenStable();

        expect(checkConsent).not.toHaveBeenCalled();
        expect(aiFoodService.parseFoodText).not.toHaveBeenCalled();
        enterRecognitionText(fixture, '  eggs  ');

        await vi.waitFor(() => {
            expect(aiFoodService.parseFoodText).toHaveBeenCalledExactlyOnceWith({ text: 'eggs' });
        });
    });
});

describe('AiInputBarComponent history access', () => {
    it('opens recent recognitions from the food input', async () => {
        const { dialogService, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();
        const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button'));
        const historyButton = buttons.find(button => button.textContent.includes('AI_RECOGNITION.SHOW_RECENT'));

        expect(historyButton).toBeDefined();
        expect(historyButton?.getAttribute('aria-label')).toBe('AI_RECOGNITION.SHOW_RECENT');
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

describe('Premium prompts from the rendered AI composer', () => {
    afterEach(() => {
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
    });

    it('dismisses the camera upgrade prompt without opening the file picker or requesting consent', async () => {
        vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
        const { fixture, navigationService } = await setupAiInputBarAsync('emit', { isPremium: false, overlayUpload: NEVER });
        const askHealthConsent = vi.spyOn(TestBed.inject(PrivacyConsentFacade), 'ensureHealthConsentAsync');
        const dialogs = TestBed.inject(FdUiDialogService);
        fixture.detectChanges();
        const camera = selectRecognitionControl(fixture, 'button[aria-label="AI_INPUT_BAR.PHOTO_TITLE"]', HTMLButtonElement);
        const fileInput = selectRecognitionControl(fixture, 'input[type="file"]', HTMLInputElement);
        const openNativePicker = vi.spyOn(fileInput, 'click').mockImplementation(() => {});

        try {
            camera.click();
            await vi.waitFor(() => {
                expect(document.querySelector('fd-premium-required-dialog')?.textContent).toContain('AI.PREMIUM_DIALOG.TITLE');
            });
            const cancel = Array.from(document.querySelectorAll<HTMLButtonElement>('fd-premium-required-dialog button')).find(button =>
                button.textContent.includes('AI.PREMIUM_DIALOG.CANCEL'),
            );
            expect(cancel).toBeDefined();
            cancel?.click();
            await vi.waitFor(() => {
                expect(document.querySelector('fd-premium-required-dialog')).toBeNull();
            });
            expect(askHealthConsent).not.toHaveBeenCalled();
            expect(openNativePicker).not.toHaveBeenCalled();
            expect(navigationService.navigateToPremiumAccessAsync).not.toHaveBeenCalled();
        } finally {
            dialogs.closeAll();
        }
    });

    it('closes the microphone upgrade prompt when the composer is removed', async () => {
        vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
        const { fixture, navigationService, speechRecognition } = await setupAiInputBarAsync('emit', {
            isPremium: false,
            speechSupported: true,
            overlayUpload: NEVER,
        });
        const askHealthConsent = vi.spyOn(TestBed.inject(PrivacyConsentFacade), 'ensureHealthConsentAsync');
        const dialogs = TestBed.inject(FdUiDialogService);
        fixture.detectChanges();
        const microphone = selectRecognitionControl(fixture, 'button[aria-label="MEAL_LIST.VOICE_MIC_TITLE"]', HTMLButtonElement);

        try {
            microphone.click();
            await vi.waitFor(() => {
                expect(document.querySelector('fd-premium-required-dialog')?.textContent).toContain('AI.PREMIUM_DIALOG.TITLE');
            });
            fixture.destroy();
            await vi.waitFor(() => {
                expect(document.querySelector('fd-premium-required-dialog')).toBeNull();
            });
            expect(askHealthConsent).not.toHaveBeenCalled();
            expect(speechRecognition.start).not.toHaveBeenCalled();
            expect(navigationService.navigateToPremiumAccessAsync).not.toHaveBeenCalled();
        } finally {
            dialogs.closeAll();
        }
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
        expect(component['photoSelection']()).toEqual(imageSelection(RECENT_JOB.imageUrl, RECENT_JOB.imageAssetId));
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
        component['photoSelection'].set(imageSelection(RECENT_JOB.imageUrl, RECENT_JOB.imageAssetId));
        fixture.detectChanges();

        await component['onPhotoReanalyzeAsync']();

        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
    });

    it('preserves the saved result when consent for another analysis is declined', async () => {
        const { aiFoodService, component, dialogService, fixture, userFacade } = await setupAiInputBarAsync('create', {
            aiConsentAcceptedAt: null,
        });
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(false) });
        component['photoSelection'].set(imageSelection(RECENT_JOB.imageUrl, RECENT_JOB.imageAssetId));
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
        component['photoSelection'].set(imageSelection(RECENT_JOB.imageUrl, RECENT_JOB.imageAssetId));
        fixture.detectChanges();
        const reanalysis = component['onPhotoReanalyzeAsync']();
        await vi.waitUntil(() => userFacade.getInfoSilently.mock.calls.length === 1);
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
        expect(component['photoSelection']()).toEqual(imageSelection('blob:second-photo', null));
        expect(component['photoDialogRef']()).not.toBeNull();
    });
});

describe('AiInputBarComponent photo lifecycle', () => {
    it.each(['preparation', 'analysis', 'nutrition', 'complete'] as const)(
        'closes the photo overlay and clears private data on owner destruction during %s',
        async stage => {
            const { aiFoodService, component, dialogService, fixture } = await setupAiInputBarAsync();
            const close = vi.fn();
            const closed = new Subject<boolean | FoodRecognitionJob | undefined>();
            const analysis = new Subject<FoodVisionResponse>();
            const nutrition = new Subject<FoodNutritionResponse>();
            dialogService.open.mockReturnValueOnce({ componentInstance: null, close, afterClosed: () => closed });
            if (stage === 'analysis') {
                aiFoodService.analyzeFoodImage.mockReturnValueOnce(analysis);
            } else if (stage === 'nutrition') {
                aiFoodService.calculateNutrition.mockReturnValueOnce(nutrition);
            }
            fixture.detectChanges();
            component['onPhotoPreparationStarted']('blob:private-photo');
            if (stage !== 'preparation') {
                component['onPhotoSelected'](imageSelection('https://example.com/private-photo.jpg', 'asset-1'));
            }
            const dialogConfig = dialogService.open.mock.calls[0][1] as {
                data: {
                    imageUrl: () => string | null;
                    results: () => FoodVisionItem[];
                    nutrition: () => FoodNutritionResponse | null;
                    isPreparing: () => boolean;
                };
            };

            fixture.destroy();
            analysis.next({ items: VISION_ITEMS });
            nutrition.next(NUTRITION);
            closed.next(undefined);

            expect(close).toHaveBeenCalledOnce();
            expect(component['photoDialogRef']()).toBeNull();
            expect(dialogConfig.data.imageUrl()).toBeNull();
            expect(dialogConfig.data.results()).toEqual([]);
            expect(dialogConfig.data.nutrition()).toBeNull();
            expect(dialogConfig.data.isPreparing()).toBe(false);
            expect(analysis.observed).toBe(false);
            expect(nutrition.observed).toBe(false);
            expect(closed.observed).toBe(false);
        },
    );

    it('does not close an already dismissed photo overlay again on owner destruction', async () => {
        const { component, dialogService, fixture } = await setupAiInputBarAsync();
        const close = vi.fn();
        dialogService.open.mockReturnValueOnce({ componentInstance: null, close, afterClosed: () => NEVER });
        fixture.detectChanges();
        component['onPhotoPreparationStarted']('blob:private-photo');

        component['dismissPhotoResult']();
        fixture.destroy();

        expect(close).toHaveBeenCalledOnce();
        expect(component['photoDialogRef']()).toBeNull();
        expect(component['hasPhotoResult']()).toBe(false);
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
        expect(component['photoSelection']()).toEqual(imageSelection('blob:local-preview', null));
        expect(component['photoIsPreparing']()).toBe(true);
        expect(component['isDisabled']()).toBe(true);
        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
    });

    it('ignores photo selections without asset id', async () => {
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();

        component['onPhotoSelected'](imageSelection('https://example.com/photo.jpg', null));

        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
        expect(component['hasPhotoResult']()).toBe(false);
    });

    it('runs photo recognition when asset id is present', async () => {
        const { aiFoodService, component, dialogService, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();

        component['onPhotoSelected'](imageSelection('https://example.com/photo.jpg', 'asset-1'));

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

        component['onPhotoSelected'](imageSelection('https://cdn.example.com/photo.jpg', 'asset-1'));

        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(component['photoSelection']()).toEqual(imageSelection('https://cdn.example.com/photo.jpg', 'asset-1'));
        expect(dialogConfig.data.imageUrl()).toBe('https://cdn.example.com/photo.jpg');
        expect(dialogConfig.data.isPreparing()).toBe(false);
    });

    it('emits recognized photo meal with image metadata', async () => {
        const { component, fixture } = await setupAiInputBarAsync();
        const recognizedSpy = vi.fn<(result: AiInputBarResult) => void>();
        component['mealRecognized'].subscribe(result => {
            recognizedSpy(result);
        });
        component['photoSelection'].set(imageSelection('https://example.com/photo.jpg', 'asset-1'));
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

        component['onPhotoSelected'](imageSelection('https://example.com/photo.jpg', 'asset-1'));

        expect(component['photoNutritionErrorKey']()).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA');
        expect(component['photoIsNutritionLoading']()).toBe(false);
    });
});

describe('AI input native recognition controls', () => {
    it('uses consent already recorded on the freshly loaded account', async () => {
        const { aiFoodService, dialogService, fixture, userFacade } = await setupAiInputBarAsync('emit', { aiConsentAcceptedAt: null });
        userFacade.getInfoSilently.mockReturnValueOnce(of({ aiConsentAcceptedAt: '2026-05-17T00:00:00Z' }));
        fixture.detectChanges();

        enterRecognitionText(fixture, ' eggs ');

        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(aiFoodService.parseFoodText).toHaveBeenCalledExactlyOnceWith({ text: 'eggs' });
            expect(selectRecognitionControl(fixture, '.ai-input-bar__result-panel', HTMLElement).textContent).toContain('eggs');
        });
        expect(dialogService.open).not.toHaveBeenCalled();
        expect(userFacade.acceptAiConsent).not.toHaveBeenCalled();
    });

    it('keeps typed food when health consent is declined', async () => {
        const { aiFoodService, dialogService, fixture, userFacade } = await setupAiInputBarAsync('emit', { aiConsentAcceptedAt: null });
        const askHealthConsent = vi.spyOn(TestBed.inject(PrivacyConsentFacade), 'ensureHealthConsentAsync').mockResolvedValueOnce(false);
        fixture.detectChanges();

        const input = enterRecognitionText(fixture, 'eggs');
        await fixture.whenStable();

        expect(input.value).toBe('eggs');
        expect(askHealthConsent).toHaveBeenCalledExactlyOnceWith(true);
        expect(userFacade.getInfoSilently).not.toHaveBeenCalled();
        expect(dialogService.open).not.toHaveBeenCalled();
        expect(aiFoodService.parseFoodText).not.toHaveBeenCalled();
    });

    it('opens the native photo picker only after recognition consent succeeds', async () => {
        const { aiFoodService, fixture } = await setupAiInputBarAsync();
        const askHealthConsent = vi.spyOn(TestBed.inject(PrivacyConsentFacade), 'ensureHealthConsentAsync').mockResolvedValueOnce(false);
        fixture.detectChanges();
        const fileInput = selectRecognitionControl(fixture, 'input[type="file"]', HTMLInputElement);
        const openNativePicker = vi.spyOn(fileInput, 'click').mockImplementation(() => {});
        const camera = selectRecognitionControl(fixture, 'button[aria-label="AI_INPUT_BAR.PHOTO_TITLE"]', HTMLButtonElement);

        fixture.componentRef.setInput('isProcessing', true);
        expect(camera.disabled).toBe(false);
        camera.click();
        expect(askHealthConsent).not.toHaveBeenCalled();
        expect(openNativePicker).not.toHaveBeenCalled();
        fixture.detectChanges();
        expect(camera.disabled).toBe(true);

        fixture.componentRef.setInput('isProcessing', false);
        fixture.detectChanges();
        camera.click();
        await fixture.whenStable();

        expect(openNativePicker).not.toHaveBeenCalled();

        camera.click();

        await vi.waitFor(() => {
            expect(openNativePicker).toHaveBeenCalledOnce();
        });
        expect(askHealthConsent).toHaveBeenCalledTimes(2);
        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
    });

    it('ignores a product label selected from meal recognition history', async () => {
        const { aiFoodService, dialogService, fixture } = await setupAiInputBarAsync();
        const selectedJob = new Subject<FoodRecognitionJob>();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => selectedJob });
        fixture.detectChanges();
        const history = selectRecognitionControl(fixture, 'button[aria-label="AI_RECOGNITION.SHOW_RECENT"]', HTMLButtonElement);

        history.click();
        history.click();
        selectedJob.next({ ...RECENT_JOB, isProductLabel: true });
        selectedJob.complete();

        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(aiFoodService.resumeRecognition).not.toHaveBeenCalled();

        history.click();

        expect(dialogService.open).toHaveBeenCalledTimes(2);
    });
});

describe('AI input consent and result ownership', () => {
    it('does not open AI consent when the account response arrives after leaving the composer', async () => {
        const { aiFoodService, dialogService, fixture, userFacade } = await setupAiInputBarAsync('emit', { aiConsentAcceptedAt: null });
        const currentUser = new Subject<{ aiConsentAcceptedAt: string | null }>();
        userFacade.getInfoSilently.mockReturnValueOnce(currentUser);
        fixture.detectChanges();

        enterRecognitionText(fixture, 'eggs');
        await vi.waitFor(() => {
            expect(userFacade.getInfoSilently).toHaveBeenCalledOnce();
        });
        fixture.destroy();
        currentUser.next({ aiConsentAcceptedAt: null });
        await waitForAsyncTasksAsync();

        expect(currentUser.observed).toBe(false);
        expect(dialogService.open).not.toHaveBeenCalled();
        expect(userFacade.acceptAiConsent).not.toHaveBeenCalled();
        expect(aiFoodService.parseFoodText).not.toHaveBeenCalled();
    });

    it('closes the AI consent prompt when leaving and ignores any late confirmation', async () => {
        const { aiFoodService, dialogService, fixture, userFacade } = await setupAiInputBarAsync('emit', { aiConsentAcceptedAt: null });
        const accepted = new Subject<boolean>();
        const close = vi.fn();
        dialogService.open.mockReturnValueOnce({ close, afterClosed: () => accepted });
        fixture.detectChanges();

        enterRecognitionText(fixture, 'eggs');
        await vi.waitFor(() => {
            expect(dialogService.open).toHaveBeenCalledOnce();
        });
        fixture.destroy();
        accepted.next(true);
        await waitForAsyncTasksAsync();

        expect(close).toHaveBeenCalledOnce();
        expect(accepted.observed).toBe(false);
        expect(userFacade.acceptAiConsent).not.toHaveBeenCalled();
        expect(aiFoodService.parseFoodText).not.toHaveBeenCalled();
    });

    it('reruns analysis for the text shown in the result even if the input is edited', async () => {
        const { aiFoodService, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();
        const input = enterRecognitionText(fixture, 'eggs');
        await vi.waitFor(() => {
            expect(aiFoodService.parseFoodText).toHaveBeenCalledOnce();
        });
        fixture.detectChanges();

        input.value = 'salad';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        selectRecognitionControl(fixture, 'button[aria-label="MEAL_MANAGE.PHOTO_AI_DIALOG.REANALYZE_ACTION"]', HTMLButtonElement).click();
        await vi.waitFor(() => {
            expect(aiFoodService.parseFoodText).toHaveBeenCalledTimes(2);
        });

        expect(aiFoodService.parseFoodText).toHaveBeenNthCalledWith(2, { text: 'eggs' });
        expect(input.value).toBe('salad');
    });
});

describe('AI input deferred actions after closing the composer', () => {
    it('preserves a collapsed draft without analyzing it after account consent is loaded', async () => {
        const { aiFoodService, fixture, userFacade } = await setupAiInputBarAsync('create', { aiConsentAcceptedAt: null });
        const currentUser = new Subject<{ aiConsentAcceptedAt: string }>();
        const acceptedConsent = { aiConsentAcceptedAt: '2026-05-17T00:00:00Z' };
        userFacade.getInfoSilently.mockReturnValueOnce(currentUser);
        fixture.detectChanges();

        const input = enterRecognitionText(fixture, 'eggs');
        await vi.waitFor(() => {
            expect(userFacade.getInfoSilently).toHaveBeenCalledOnce();
        });
        fixture.componentRef.setInput('active', false);
        fixture.detectChanges();
        currentUser.next(acceptedConsent);
        currentUser.complete();
        await waitForAsyncTasksAsync();

        expect(aiFoodService.parseFoodText).not.toHaveBeenCalled();
        expect(input.value).toBe('eggs');
        userFacade.user.set(acceptedConsent);
        fixture.componentRef.setInput('active', true);
        fixture.detectChanges();
        enterRecognitionText(fixture, 'eggs');
        await vi.waitFor(() => {
            expect(aiFoodService.parseFoodText).toHaveBeenCalledExactlyOnceWith({ text: 'eggs' });
        });
    });

    it('keeps the native photo picker closed when health consent arrives after collapse', async () => {
        const { fixture } = await setupAiInputBarAsync('create');
        const permission = new Subject<boolean>();
        const requestConsent = vi
            .spyOn(TestBed.inject(PrivacyConsentFacade), 'ensureHealthConsentAsync')
            .mockImplementationOnce(async () => firstValueFrom(permission));
        fixture.detectChanges();
        const photo = selectRecognitionControl(fixture, 'button[aria-label="AI_INPUT_BAR.PHOTO_TITLE"]', HTMLButtonElement);
        const input = selectRecognitionControl(fixture, 'input[type="file"]', HTMLInputElement);
        const picker = vi.spyOn(input, 'click').mockImplementation(() => {});

        photo.click();
        expect(requestConsent).toHaveBeenCalledOnce();
        fixture.componentRef.setInput('active', false);
        fixture.detectChanges();
        permission.next(true);
        permission.complete();
        await waitForAsyncTasksAsync();

        expect(picker).not.toHaveBeenCalled();
        fixture.componentRef.setInput('active', true);
        fixture.detectChanges();
        photo.click();
        await vi.waitFor(() => {
            expect(picker).toHaveBeenCalledOnce();
        });
    });
});

it('discards the final transcript of a collapsed composer and accepts manual stop in a new recording', async () => {
    const { aiFoodService, fixture, speechRecognition } = await setupAiInputBarAsync('create', { speechSupported: true });
    fixture.detectChanges();
    const microphone = selectRecognitionControl(fixture, 'button[aria-label="MEAL_LIST.VOICE_MIC_TITLE"]', HTMLButtonElement);

    microphone.click();
    await fixture.whenStable();
    expect(speechRecognition.start).toHaveBeenCalledOnce();
    const firstTranscript = speechRecognition.start.mock.calls[0][1];

    fixture.componentRef.setInput('active', false);
    fixture.detectChanges();
    expect(speechRecognition.isListening()).toBe(false);
    fixture.componentRef.setInput('active', true);
    fixture.detectChanges();
    firstTranscript('abandoned snack');
    await waitForAsyncTasksAsync();
    fixture.detectChanges();

    expect(aiFoodService.parseFoodText).not.toHaveBeenCalled();
    expect(selectRecognitionControl(fixture, '.ai-input-bar__input', HTMLInputElement).value).toBe('');

    microphone.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(speechRecognition.start).toHaveBeenCalledTimes(2);
    microphone.click();
    expect(speechRecognition.isListening()).toBe(false);
    speechRecognition.start.mock.calls[1][1]('two eggs');
    await vi.waitFor(() => {
        expect(aiFoodService.parseFoodText).toHaveBeenCalledExactlyOnceWith({ text: 'two eggs' });
    });
});

it('disables sending while text recognition is pending and recovers the control after an error', async () => {
    const { aiFoodService, fixture } = await setupAiInputBarAsync();
    const recognition = new Subject<FoodVisionResponse>();
    aiFoodService.parseFoodText.mockReturnValueOnce(recognition);
    fixture.detectChanges();
    const input = enterRecognitionText(fixture, 'breakfast');
    await vi.waitFor(() => {
        expect(aiFoodService.parseFoodText).toHaveBeenCalledExactlyOnceWith({ text: 'breakfast' });
    });
    fixture.detectChanges();
    const send = selectRecognitionControl(fixture, 'button[aria-label="AI_INPUT_BAR.ADD_ACTION"]', HTMLButtonElement);
    expect(send.disabled).toBe(true);
    expect(send.querySelector('fd-ui-icon')?.textContent).toContain('hourglass_empty');
    expect(input.disabled).toBe(true);

    recognition.error(new Error('Recognition is temporarily unavailable'));
    fixture.detectChanges();
    expect(send.disabled).toBe(false);
    expect(send.querySelector('fd-ui-icon')?.textContent).toContain('send');
    expect(input.value).toBe('breakfast');
});

describe('Recognition actions while health consent is pending', () => {
    it('starts one recognition when the user submits the same draft again before consent completes', async () => {
        const { aiFoodService, fixture } = await setupAiInputBarAsync();
        const permission = new Subject<boolean>();
        const recognition = new Subject<FoodVisionResponse>();
        vi.spyOn(TestBed.inject(PrivacyConsentFacade), 'ensureHealthConsentAsync').mockImplementation(async () =>
            firstValueFrom(permission),
        );
        aiFoodService.parseFoodText.mockReturnValue(recognition);
        fixture.detectChanges();

        enterRecognitionText(fixture, 'oatmeal and blueberries');
        fixture.detectChanges();
        selectRecognitionControl(fixture, 'button[aria-label="AI_INPUT_BAR.ADD_ACTION"]', HTMLButtonElement).click();
        expect(aiFoodService.parseFoodText).not.toHaveBeenCalled();
        permission.next(true);
        permission.complete();
        await vi.waitFor(() => {
            expect(aiFoodService.parseFoodText).toHaveBeenCalled();
        });

        expect(aiFoodService.parseFoodText).toHaveBeenCalledExactlyOnceWith({ text: 'oatmeal and blueberries' });
    });

    it('opens one native file picker for repeated photo clicks before health consent completes', async () => {
        const { fixture } = await setupAiInputBarAsync();
        const permission = new Subject<boolean>();
        vi.spyOn(TestBed.inject(PrivacyConsentFacade), 'ensureHealthConsentAsync').mockImplementation(async () =>
            firstValueFrom(permission),
        );
        fixture.detectChanges();
        const photo = selectRecognitionControl(fixture, 'button[aria-label="AI_INPUT_BAR.PHOTO_TITLE"]', HTMLButtonElement);
        const fileInput = selectRecognitionControl(fixture, 'input[type="file"]', HTMLInputElement);
        const picker = vi.spyOn(fileInput, 'click').mockImplementation(() => {});

        photo.click();
        photo.click();
        expect(picker).not.toHaveBeenCalled();
        permission.next(true);
        permission.complete();
        await vi.waitFor(() => {
            expect(picker).toHaveBeenCalled();
        });

        expect(picker).toHaveBeenCalledOnce();
    });
});

describe('AI input dismissal during reanalysis', () => {
    it('does not request another analysis after the user closes the result while consent is pending', async () => {
        const { aiFoodService, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();
        enterRecognitionText(fixture, 'eggs');
        await vi.waitFor(() => {
            expect(aiFoodService.parseFoodText).toHaveBeenCalledOnce();
        });
        fixture.detectChanges();
        const permission = new Subject<boolean>();
        const requestConsent = vi
            .spyOn(TestBed.inject(PrivacyConsentFacade), 'ensureHealthConsentAsync')
            .mockClear()
            .mockImplementationOnce(async () => firstValueFrom(permission));
        const result = '.ai-input-bar__result-panel';

        selectRecognitionControl(fixture, 'button[aria-label="MEAL_MANAGE.PHOTO_AI_DIALOG.REANALYZE_ACTION"]', HTMLButtonElement).click();
        expect(requestConsent).toHaveBeenCalledOnce();
        selectRecognitionControl(fixture, `${result} button[aria-label="COMMON.CANCEL"]`, HTMLButtonElement).click();
        fixture.detectChanges();
        permission.next(true);
        permission.complete();
        await new Promise<void>(resolve => {
            setTimeout(resolve);
        });
        fixture.detectChanges();

        expect(aiFoodService.parseFoodText).toHaveBeenCalledExactlyOnceWith({ text: 'eggs' });
        expect((fixture.nativeElement as HTMLElement).querySelector(result)).toBeNull();
    });

    it('ignores queued Add and retry actions before a dismissed text result is removed', async () => {
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync();
        const recognized = vi.fn<(value: AiInputBarResult) => void>();
        component.mealRecognized.subscribe(recognized);
        fixture.detectChanges();
        const input = enterRecognitionText(fixture, 'egg');
        const panel = '.ai-input-bar__result-panel';
        const add = await vi.waitFor(() => {
            fixture.detectChanges();
            const button = selectRecognitionControl(fixture, `${panel} .ai-photo-result__submit-action button`, HTMLButtonElement);
            expect(button.disabled).toBe(false);
            return button;
        });
        const retry = selectRecognitionControl(
            fixture,
            `${panel} button[aria-label="MEAL_MANAGE.PHOTO_AI_DIALOG.REANALYZE_ACTION"]`,
            HTMLButtonElement,
        );
        selectRecognitionControl(fixture, `${panel} button[aria-label="COMMON.CANCEL"]`, HTMLButtonElement).click();

        expect(add.disabled || retry.disabled).toBe(false);
        add.click();
        retry.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(recognized).not.toHaveBeenCalled();
        expect(aiFoodService.parseFoodText).toHaveBeenCalledExactlyOnceWith({ text: 'egg' });
        expect(input.value).toBe('egg');
        expect((fixture.nativeElement as HTMLElement).querySelector(panel)).toBeNull();
    });
});

function enterRecognitionText(fixture: ComponentFixture<AiInputBarComponent>, text: string): HTMLInputElement {
    const input = selectRecognitionControl(fixture, '.ai-input-bar__input', HTMLInputElement);
    input.value = text;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
    return input;
}

function selectRecognitionControl<T extends HTMLElement>(
    fixture: ComponentFixture<AiInputBarComponent>,
    selector: string,
    elementType: typeof HTMLElement & { prototype: T },
): T {
    const host = fixture.nativeElement as HTMLElement;
    const element = host.querySelector<T>(selector);
    if (!(element instanceof elementType)) {
        throw new Error(`Expected a rendered recognition control matching ${selector}`);
    }
    return element;
}

describe('AI input photo result overlay', () => {
    afterEach(() => {
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
    });

    it('uses the uploaded photo in the rendered result, then returns one reviewed meal', async () => {
        const uploaded = new Subject<ImageSelection>();
        vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
        vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:food-preview');
        vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync('emit', { overlayUpload: uploaded });
        const recognized = vi.fn<(value: AiInputBarResult) => void>();
        component.mealRecognized.subscribe(recognized);
        fixture.detectChanges();
        const fileInput = selectRecognitionControl(fixture, 'input[type="file"]', HTMLInputElement);

        Object.defineProperty(fileInput, 'files', { configurable: true, value: [new File(['sample'], 'food.gif', { type: 'image/gif' })] });
        fileInput.dispatchEvent(new Event('change', { bubbles: true }));

        await vi.waitFor(() => {
            TestBed.tick();
            expect(document.querySelector('fd-ai-photo-result img')?.getAttribute('src')).toBe('blob:food-preview');
            expect(document.querySelector<HTMLButtonElement>('fd-ai-photo-result .ai-photo-result__submit-action button')?.disabled).toBe(
                true,
            );
        });
        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();

        uploaded.next(imageSelection(RECENT_JOB.imageUrl, RECENT_JOB.imageAssetId));
        uploaded.complete();
        await vi.waitFor(() => {
            TestBed.tick();
            expect(document.querySelector('fd-ai-photo-result img')?.getAttribute('src')).toBe(RECENT_JOB.imageUrl);
            expect(document.querySelector<HTMLButtonElement>('fd-ai-photo-result .ai-photo-result__submit-action button')?.disabled).toBe(
                false,
            );
        });

        const retry = document.querySelector<HTMLButtonElement>(
            'fd-ai-photo-result button[aria-label="MEAL_MANAGE.PHOTO_AI_DIALOG.REANALYZE_ACTION"]',
        );
        if (retry === null) {
            throw new Error('Expected the rendered photo retry action');
        }
        retry.click();
        await vi.waitFor(() => {
            TestBed.tick();
            expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledTimes(2);
        });
        document.querySelector<HTMLButtonElement>('fd-ai-photo-result .ai-photo-result__submit-action button')?.click();

        expect(recognized).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                source: 'Photo',
                imageAssetId: RECENT_JOB.imageAssetId,
                imageUrl: RECENT_JOB.imageUrl,
                items: [expect.objectContaining({ nameEn: 'egg', calories: NUTRITION.calories })],
            }),
        );
        await vi.waitFor(() => {
            expect(document.querySelector('fd-ai-photo-result')).toBeNull();
        });
    });
});

describe('AI input photo upload dismissal', () => {
    afterEach(() => {
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
    });

    it.each(['before the next render', 'after the next render'])(
        'ignores an abandoned upload that resolves %s and accepts the next file',
        async completion => {
            const abandoned = new Subject<ImageSelection>();
            const nextUpload = new Subject<ImageSelection>();
            const { fixture, aiFoodService } = await setupPhotoUploadPreviewAsync(abandoned, nextUpload);
            selectPreviewImage(fixture, 'abandoned.gif');
            await waitForPhotoPreviewAsync('blob:food-preview');
            expect(abandoned.observed).toBe(true);

            const cancel = photoPreviewButton('button[aria-label="COMMON.CANCEL"]');
            cancel.click();
            if (completion === 'after the next render') {
                TestBed.tick();
                expect(abandoned.observed).toBe(false);
            }
            abandoned.next(imageSelection('https://example.test/abandoned.gif', 'abandoned-asset'));
            abandoned.complete();
            TestBed.tick();
            expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
            expect(document.querySelector('fd-ai-photo-result')).toBeNull();

            selectPreviewImage(fixture, 'retained.gif');
            await waitForPhotoPreviewAsync('blob:food-preview');
            expect(nextUpload.observed).toBe(true);
            nextUpload.next(imageSelection(RECENT_JOB.imageUrl, RECENT_JOB.imageAssetId));
            nextUpload.complete();
            await waitForPhotoPreviewAsync(RECENT_JOB.imageUrl);
            expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledExactlyOnceWith({ imageAssetId: RECENT_JOB.imageAssetId });
        },
    );

    it('announces an upload error without analyzing the failed file and re-enables the camera after dismissal', async () => {
        const failedUpload = new Subject<ImageSelection>();
        const { fixture, aiFoodService } = await setupPhotoUploadPreviewAsync(failedUpload);
        const camera = selectRecognitionControl(fixture, 'button[aria-label="AI_INPUT_BAR.PHOTO_TITLE"]', HTMLButtonElement);
        selectPreviewImage(fixture, 'failed.gif');
        await waitForPhotoPreviewAsync('blob:food-preview');

        failedUpload.error(new Error('Upload unavailable'));
        await vi.waitFor(() => {
            TestBed.tick();
            expect(document.querySelector('fd-ai-photo-result [role="alert"]')?.textContent).toContain(
                'IMAGE_UPLOAD_FIELD.ERRORS.UPLOAD_FAILED',
            );
        });
        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
        expect(photoPreviewButton('.ai-photo-result__submit-action button').disabled).toBe(true);
        photoPreviewButton('button[aria-label="COMMON.CANCEL"]').click();
        TestBed.tick();
        expect(document.querySelector('fd-ai-photo-result')).toBeNull();
        expect(camera.disabled).toBe(false);
    });
});

describe('AI input photo overlay keyboard dismissal', () => {
    afterEach(() => {
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
    });

    it('closes a pending photo with Escape and analyzes only the next selected file', async () => {
        const abandoned = new Subject<ImageSelection>();
        const nextUpload = new Subject<ImageSelection>();
        const { fixture, aiFoodService } = await setupPhotoUploadPreviewAsync(abandoned, nextUpload);
        selectPreviewImage(fixture, 'abandoned.gif');
        await waitForPhotoPreviewAsync('blob:food-preview');
        expect(abandoned.observed).toBe(true);

        const escapeKeyCode = 27;
        const cancel = photoPreviewButton('button[aria-label="COMMON.CANCEL"]');
        cancel.focus();
        cancel.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: escapeKeyCode, bubbles: true, cancelable: true }));
        await vi.waitFor(() => {
            TestBed.tick();
            expect(document.querySelector('fd-ai-photo-result')).toBeNull();
            expect(abandoned.observed).toBe(false);
        });
        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();

        selectPreviewImage(fixture, 'next.gif');
        await waitForPhotoPreviewAsync('blob:food-preview');
        expect(nextUpload.observed).toBe(true);
        nextUpload.next(imageSelection(RECENT_JOB.imageUrl, RECENT_JOB.imageAssetId));
        nextUpload.complete();
        await waitForPhotoPreviewAsync(RECENT_JOB.imageUrl);

        expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledExactlyOnceWith({ imageAssetId: RECENT_JOB.imageAssetId });
        photoPreviewButton('button[aria-label="COMMON.CANCEL"]').click();
        TestBed.tick();
        expect(document.querySelector('fd-ai-photo-result')).toBeNull();
    });
});

describe('AI input uploaded asset retention', () => {
    afterEach(() => {
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
    });

    it('keeps the asset submitted with the previous meal when a later upload is dismissed', async () => {
        const first = new Subject<ImageSelection>();
        const next = new Subject<ImageSelection>();
        const { fixture, component } = await setupPhotoUploadPreviewAsync(first, next);
        const accepted = vi.fn<(value: AiInputBarResult) => void>();
        component.mealRecognized.subscribe(accepted);
        const deleteAsset = vi.spyOn(TestBed.inject(ImageUploadFacade), 'deleteAsset');
        selectPreviewImage(fixture, 'accepted.gif');
        await waitForPhotoPreviewAsync('blob:food-preview');
        first.next(imageSelection(RECENT_JOB.imageUrl, RECENT_JOB.imageAssetId));
        first.complete();
        await waitForPhotoPreviewAsync(RECENT_JOB.imageUrl);
        photoPreviewButton('.ai-photo-result__submit-action button').click();
        expect(accepted).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ imageAssetId: RECENT_JOB.imageAssetId }));

        selectPreviewImage(fixture, 'dismissed.gif');
        await waitForPhotoPreviewAsync('blob:food-preview');
        expect(next.observed).toBe(true);
        photoPreviewButton('button[aria-label="COMMON.CANCEL"]').click();
        TestBed.tick();
        expect(next.observed).toBe(false);
        expect(deleteAsset).not.toHaveBeenCalled();
    });
});

async function setupPhotoUploadPreviewAsync(
    upload: Subject<ImageSelection>,
    nextUpload?: Subject<ImageSelection>,
): Promise<AiInputBarTestContext> {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:food-preview');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    const context = await setupAiInputBarAsync('emit', { overlayUpload: upload });
    if (nextUpload !== undefined) {
        vi.spyOn(TestBed.inject(ImageUploadFacade), 'upload').mockReturnValueOnce(upload).mockReturnValueOnce(nextUpload);
    }
    context.fixture.detectChanges();
    return context;
}

function selectPreviewImage(fixture: ComponentFixture<AiInputBarComponent>, name: string): void {
    const input = selectRecognitionControl(fixture, 'input[type="file"]', HTMLInputElement);
    Object.defineProperty(input, 'files', { configurable: true, value: [new File(['sample'], name, { type: 'image/gif' })] });
    input.dispatchEvent(new Event('change', { bubbles: true }));
}

async function waitForPhotoPreviewAsync(src: string): Promise<void> {
    await vi.waitFor(() => {
        TestBed.tick();
        expect(document.querySelector('fd-ai-photo-result img')?.getAttribute('src')).toBe(src);
    });
}

function photoPreviewButton(selector: string): HTMLButtonElement {
    const button = document.querySelector(`fd-ai-photo-result ${selector}`);
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error(`Expected a photo preview button matching ${selector}`);
    }
    return button;
}

describe('AI input meal review before submission', () => {
    afterEach(() => {
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
    });

    it.each(['text', 'photo'] as const)('recalculates a renamed %s item before submitting the reviewed meal', async source => {
        const uploaded = new Subject<ImageSelection>();
        const { aiFoodService, component, fixture } =
            source === 'photo' ? await setupPhotoUploadPreviewAsync(uploaded) : await setupAiInputBarAsync();
        const recognized = vi.fn<(value: AiInputBarResult) => void>();
        component.mealRecognized.subscribe(recognized);
        const recalculation = new Subject<FoodNutritionResponse>();
        aiFoodService.calculateNutrition.mockReturnValueOnce(of(NUTRITION)).mockReturnValueOnce(recalculation);
        if (source === 'photo') {
            selectPreviewImage(fixture, 'meal-to-review.gif');
            await waitForPhotoPreviewAsync('blob:food-preview');
            uploaded.next(imageSelection(RECENT_JOB.imageUrl, RECENT_JOB.imageAssetId));
            uploaded.complete();
        } else {
            enterRecognitionText(fixture, 'egg');
        }

        const owner: ParentNode = source === 'photo' ? document : (fixture.nativeElement as HTMLElement);
        const result = await vi.waitFor(() => {
            TestBed.tick();
            fixture.detectChanges();
            const panel = owner.querySelector<HTMLElement>('fd-ai-photo-result');
            expect(panel?.querySelector<HTMLButtonElement>('.ai-photo-result__submit-action button')?.disabled).toBe(false);
            if (panel === null) {
                throw new Error('Expected a rendered recognition result');
            }
            return panel;
        });
        reviewAction(result, 'MEAL_MANAGE.PHOTO_AI_DIALOG.EDIT_BUTTON').click();
        TestBed.tick();
        fixture.detectChanges();
        const name = result.querySelector<HTMLInputElement>('fd-ai-photo-edit-list input[type="text"]');
        if (name === null) {
            throw new Error('Expected the recognized food name input');
        }
        expect(name.value).toBe('egg');
        name.value = 'oatmeal';
        name.dispatchEvent(new Event('input', { bubbles: true }));
        reviewAction(result, 'MEAL_MANAGE.PHOTO_AI_DIALOG.SAVE').click();
        TestBed.tick();
        fixture.detectChanges();

        expect(aiFoodService.calculateNutrition).toHaveBeenCalledTimes(2);
        expect(aiFoodService.calculateNutrition).toHaveBeenLastCalledWith({
            items: [expect.objectContaining({ nameEn: 'oatmeal' })],
        });
        expect(recalculation.observed).toBe(true);
        const waitingSubmit = result.querySelector<HTMLButtonElement>('.ai-photo-result__submit-action button');
        expect(waitingSubmit?.disabled).toBe(true);
        waitingSubmit?.click();
        expect(recognized).not.toHaveBeenCalled();

        const correctedCalories = 68;
        recalculation.next({
            ...NUTRITION,
            calories: correctedCalories,
            items: [{ ...NUTRITION.items[0], name: 'oatmeal', calories: correctedCalories }],
        });
        recalculation.complete();
        TestBed.tick();
        fixture.detectChanges();
        const submit = result.querySelector<HTMLButtonElement>('.ai-photo-result__submit-action button');
        expect(submit?.disabled).toBe(false);
        submit?.click();
        expect(recognized).toHaveBeenCalledOnce();
        expect(recognized.mock.calls[0][0].items).toMatchObject([{ nameEn: 'oatmeal', calories: correctedCalories }]);
    });
});

describe('AI input photo reanalysis', () => {
    afterEach(() => {
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
    });

    it('ignores the previous Add action as soon as new photo analysis starts', async () => {
        const uploaded = new Subject<ImageSelection>();
        const { aiFoodService, component, fixture } = await setupPhotoUploadPreviewAsync(uploaded);
        const recognized = vi.fn<(value: AiInputBarResult) => void>();
        component.mealRecognized.subscribe(recognized);
        selectPreviewImage(fixture, 'reanalyze.gif');
        await waitForPhotoPreviewAsync('blob:food-preview');
        uploaded.next(imageSelection(RECENT_JOB.imageUrl, RECENT_JOB.imageAssetId));
        uploaded.complete();
        const addSelector = '.ai-photo-result__submit-action button';
        const add = await vi.waitFor(() => {
            TestBed.tick();
            const button = photoPreviewButton(addSelector);
            expect(button.disabled).toBe(false);
            return button;
        });
        const analysis = new Subject<FoodVisionResponse>();
        const started = new Promise<void>(resolve => {
            aiFoodService.analyzeFoodImage.mockImplementationOnce(() => {
                resolve();
                return analysis;
            });
        });
        photoPreviewButton('button[aria-label="MEAL_MANAGE.PHOTO_AI_DIALOG.REANALYZE_ACTION"]').click();
        await started;

        expect(analysis.observed).toBe(true);
        expect(add.disabled).toBe(false);
        add.click();
        expect(recognized).not.toHaveBeenCalled();

        analysis.next({ items: VISION_ITEMS });
        analysis.complete();
        TestBed.tick();
        photoPreviewButton(addSelector).click();
        expect(recognized).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ source: 'Photo', imageAssetId: RECENT_JOB.imageAssetId }),
        );
    });
});

describe('AI input after an empty recognition result', () => {
    it('skips calculation when the text response has no items and accepts a new description', async () => {
        const { aiFoodService, fixture } = await setupAiInputBarAsync();
        aiFoodService.parseFoodText.mockReturnValueOnce(of({ items: [] }));
        enterRecognitionText(fixture, 'no recognized food');
        const host = fixture.nativeElement as HTMLElement;
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(aiFoodService.parseFoodText).toHaveBeenCalledExactlyOnceWith({ text: 'no recognized food' });
            expect(host.querySelector('fd-ai-photo-result')).not.toBeNull();
        });

        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
        expect(host.querySelector('.ai-photo-result__submit-action button')).toBeNull();

        enterRecognitionText(fixture, 'egg');
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(aiFoodService.calculateNutrition).toHaveBeenCalledExactlyOnceWith({ items: VISION_ITEMS });
            expect(host.querySelector<HTMLButtonElement>('.ai-photo-result__submit-action button')?.disabled).toBe(false);
        });
    });
});

function reviewAction(result: HTMLElement, label: string): HTMLButtonElement {
    const button = [...result.querySelectorAll<HTMLButtonElement>('fd-ai-photo-result-actions button')].find(
        action => action.textContent.trim() === label,
    );
    if (button === undefined) {
        throw new Error(`Expected the recognition review action ${label}`);
    }
    return button;
}

describe('AI input history label space', () => {
    afterEach(() => {
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
    });

    it.each([
        { observesResize: true, browser: 'a browser with resize observation' },
        { observesResize: false, browser: 'a browser without resize observation' },
    ])('keeps the history action accessible when space is tight in $browser', async ({ observesResize }) => {
        const resize = new Subject<void>();
        const observe = vi.fn();
        const disconnect = vi.fn();
        class HistoryResizeObserver implements ResizeObserver {
            public constructor(callback: ResizeObserverCallback) {
                resize.subscribe(() => {
                    callback([], this);
                });
            }

            public observe = observe;
            public unobserve = vi.fn();
            public disconnect = disconnect;
        }
        vi.stubGlobal('ResizeObserver', observesResize ? HistoryResizeObserver : undefined);
        const { dialogService, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();
        const actions = selectRecognitionControl(fixture, '.ai-input-bar__actions', HTMLElement);
        const history = selectRecognitionControl(fixture, '.ai-input-bar__history', HTMLElement);
        const manual = selectRecognitionControl(fixture, '.ai-input-bar__manual-actions', HTMLElement);
        const button = selectRecognitionControl(fixture, '.ai-input-bar__history button', HTMLButtonElement);
        const label = selectRecognitionControl(fixture, '.ai-input-bar__history-label', HTMLElement);
        const icon = selectRecognitionControl(fixture, '.ai-input-bar__history fd-ui-icon', HTMLElement);
        const roomyWidth = 400;
        const crampedWidth = 100;
        const manualWidth = 120;
        const labelWidth = 88;
        const iconSize = 16;
        actions.style.padding = '0px';
        actions.style.columnGap = '8px';
        button.style.padding = '0px';
        button.style.borderWidth = '0px';
        button.style.columnGap = '8px';
        Object.defineProperty(actions, 'clientWidth', { configurable: true, value: observesResize ? roomyWidth : crampedWidth });
        vi.spyOn(manual, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, manualWidth, iconSize));
        vi.spyOn(label, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, labelWidth, iconSize));
        vi.spyOn(icon, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, iconSize, iconSize));

        fixture.componentRef.setInput('historyLabelKey', 'AI_RECOGNITION.SHOW_RECENT');
        fixture.detectChanges();

        if (!observesResize) {
            expect(history.classList.contains('ai-input-bar__history--compact')).toBe(true);
            expect(button.getAttribute('aria-label')).toBe('AI_RECOGNITION.SHOW_RECENT');
            button.click();
            expect(dialogService.open).toHaveBeenCalledExactlyOnceWith(FoodRecognitionHistoryDialogComponent, expect.anything());
            return;
        }

        expect(history.classList.contains('ai-input-bar__history--compact')).toBe(false);
        expect(observe).toHaveBeenCalledWith(actions);

        Object.defineProperty(actions, 'clientWidth', { configurable: true, value: crampedWidth });
        resize.next();
        fixture.detectChanges();

        expect(history.classList.contains('ai-input-bar__history--compact')).toBe(true);
        expect(button.getAttribute('aria-label')).toBe('AI_RECOGNITION.SHOW_RECENT');

        Object.defineProperty(actions, 'clientWidth', { configurable: true, value: roomyWidth });
        resize.next();
        fixture.detectChanges();

        expect(history.classList.contains('ai-input-bar__history--compact')).toBe(false);
        fixture.destroy();
        expect(disconnect).toHaveBeenCalledOnce();
        resize.complete();
    });
});
