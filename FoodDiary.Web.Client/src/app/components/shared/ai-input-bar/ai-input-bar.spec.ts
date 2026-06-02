import { HttpStatusCode } from '@angular/common/http';
import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import { FrontendLoggerService } from '../../../services/frontend-logger.service';
import { NavigationService } from '../../../services/navigation.service';
import { LocalizationService } from '../../../shared/i18n/localization.service';
import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import { ImageUploadFacade } from '../../../shared/lib/image-upload.facade';
import { UserFacade } from '../../../shared/lib/user.facade';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../shared/models/ai.data';
import { AiInputBarComponent } from './ai-input-bar';
import type { AiInputBarMealDetails, AiInputBarResult } from './ai-input-bar.types';

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

type AiInputBarTestContext = {
    aiFoodService: {
        analyzeFoodImage: ReturnType<typeof vi.fn>;
        calculateNutrition: ReturnType<typeof vi.fn>;
        parseFoodText: ReturnType<typeof vi.fn>;
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
};

async function setupAiInputBarAsync(
    mode: 'create' | 'emit' = 'emit',
    options: { aiConsentAcceptedAt?: string | null; isPremium?: boolean } = {},
): Promise<AiInputBarTestContext> {
    const aiFoodService = {
        parseFoodText: vi.fn().mockReturnValue(of({ items: VISION_ITEMS })),
        analyzeFoodImage: vi.fn().mockReturnValue(of({ items: VISION_ITEMS })),
        calculateNutrition: vi.fn().mockReturnValue(of(NUTRITION)),
    };
    const aiConsentAcceptedAt: string | null =
        options.aiConsentAcceptedAt === undefined ? '2026-05-17T00:00:00Z' : options.aiConsentAcceptedAt;
    const userFacade = {
        user: signal({ aiConsentAcceptedAt }),
        getInfoSilently: vi.fn().mockReturnValue(of(null)),
        acceptAiConsent: vi.fn().mockReturnValue(of(void 0)),
    };
    const navigationService = { navigateToPremiumAccessAsync: vi.fn() };
    const dialogService = { open: vi.fn((): { afterClosed: () => Observable<boolean> } => ({ afterClosed: () => of(true) })) };

    await TestBed.configureTestingModule({
        imports: [AiInputBarComponent, TranslateModule.forRoot()],
        providers: [
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
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiInputBarComponent);
    const component = fixture.componentInstance;
    fixture.componentRef.setInput('mode', mode);
    return { aiFoodService, component, dialogService, fixture, navigationService, userFacade };
}

describe('AiInputBarComponent text recognition', () => {
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

describe('AiInputBarComponent photo recognition', () => {
    it('ignores photo selections without asset id', async () => {
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();

        component['onPhotoSelected']({ url: 'https://example.com/photo.jpg', assetId: null });

        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
        expect(component['hasPhotoResult']()).toBe(false);
    });

    it('runs photo recognition when asset id is present', async () => {
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();

        component['onPhotoSelected']({ url: 'https://example.com/photo.jpg', assetId: 'asset-1' });

        expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledWith({ imageAssetId: 'asset-1' });
        expect(aiFoodService.calculateNutrition).toHaveBeenCalledWith({ items: VISION_ITEMS });
        expect(component['photoNutrition']()).toEqual(NUTRITION);
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

        expect(component['photoNutritionErrorKey']()).toBe('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA');
        expect(component['photoIsNutritionLoading']()).toBe(false);
    });
});
