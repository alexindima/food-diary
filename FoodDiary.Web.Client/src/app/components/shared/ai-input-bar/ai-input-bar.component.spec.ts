import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import { FrontendLoggerService } from '../../../services/frontend-logger.service';
import { LocalizationService } from '../../../services/localization.service';
import { NavigationService } from '../../../services/navigation.service';
import { AiFoodService } from '../../../shared/api/ai-food.service';
import { ImageUploadService } from '../../../shared/api/image-upload.service';
import { UserService } from '../../../shared/api/user.service';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../shared/models/ai.data';
import { AiInputBarComponent } from './ai-input-bar.component';
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
    fixture: ComponentFixture<AiInputBarComponent>;
};

async function setupAiInputBarAsync(mode: 'create' | 'emit' = 'emit'): Promise<AiInputBarTestContext> {
    const aiFoodService = {
        parseFoodText: vi.fn().mockReturnValue(of({ items: VISION_ITEMS })),
        analyzeFoodImage: vi.fn().mockReturnValue(of({ items: VISION_ITEMS })),
        calculateNutrition: vi.fn().mockReturnValue(of(NUTRITION)),
    };

    await TestBed.configureTestingModule({
        imports: [AiInputBarComponent, TranslateModule.forRoot()],
        providers: [
            { provide: AiFoodService, useValue: aiFoodService },
            {
                provide: UserService,
                useValue: {
                    user: signal({ aiConsentAcceptedAt: '2026-05-17T00:00:00Z' }),
                    getInfoSilently: vi.fn().mockReturnValue(of(null)),
                    acceptAiConsent: vi.fn().mockReturnValue(of(undefined)),
                },
            },
            { provide: AuthService, useValue: { isPremium: signal(true) } },
            { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } },
            { provide: NavigationService, useValue: { navigateToPremiumAccessAsync: vi.fn() } },
            { provide: FdUiDialogService, useValue: { open: vi.fn() } },
            {
                provide: ImageUploadService,
                useValue: {
                    requestUploadUrl: vi.fn(),
                    uploadToPresignedUrl: vi.fn(),
                    deleteAsset: vi.fn().mockReturnValue(of(undefined)),
                },
            },
            { provide: FrontendLoggerService, useValue: { warn: vi.fn() } },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiInputBarComponent);
    const component = fixture.componentInstance;
    fixture.componentRef.setInput('mode', mode);
    return { aiFoodService, component, fixture };
}

describe('AiInputBarComponent text recognition', () => {
    it('runs text recognition and nutrition calculation', async () => {
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync();
        component.voiceText.set(' eggs ');
        fixture.detectChanges();

        await component.submitTextAsync();

        expect(aiFoodService.parseFoodText).toHaveBeenCalledWith({ text: 'eggs' });
        expect(aiFoodService.calculateNutrition).toHaveBeenCalledWith({ items: VISION_ITEMS });
        expect(component.textSubmittedQuery()).toBe('eggs');
        expect(component.textNutrition()).toEqual(NUTRITION);
    });

    it('emits recognized meal in emit mode and clears state', async () => {
        const { component, fixture } = await setupAiInputBarAsync('emit');
        const recognizedSpy = vi.fn<(result: AiInputBarResult) => void>();
        component.mealRecognized.subscribe(result => {
            recognizedSpy(result);
        });
        component.textSubmittedQuery.set('eggs');
        component.textResults.set(VISION_ITEMS);
        component.textNutrition.set(NUTRITION);
        fixture.detectChanges();

        component.onTextAddToMeal(MEAL_DETAILS);

        expect(recognizedSpy).toHaveBeenCalledOnce();
        const recognizedResult = recognizedSpy.mock.calls[0][0];
        expect(recognizedResult.source).toBe('Text');
        expect(recognizedResult.notes).toBe('eggs');
        expect(recognizedResult.date).toBe('2026-05-17');
        expect(recognizedResult.time).toBe('09:30');
        expect(recognizedResult.recognizedAtUtc).toMatch(RECOGNIZED_AT_PATTERN);
        expect(component.voiceText()).toBe('');
        expect(component.hasTextResult()).toBe(false);
    });

    it('emits created meal in create mode', async () => {
        const { component, fixture } = await setupAiInputBarAsync('create');
        const createSpy = vi.fn<(result: AiInputBarResult) => void>();
        component.mealCreateRequested.subscribe(result => {
            createSpy(result);
        });
        component.textSubmittedQuery.set('eggs');
        component.textResults.set(VISION_ITEMS);
        component.textNutrition.set(NUTRITION);
        fixture.detectChanges();

        component.onTextAddToMeal(MEAL_DETAILS);

        expect(createSpy).toHaveBeenCalledOnce();
    });
});

describe('AiInputBarComponent photo recognition', () => {
    it('ignores photo selections without asset id', async () => {
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();

        component.onPhotoSelected({ url: 'https://example.com/photo.jpg', assetId: null });

        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
        expect(component.hasPhotoResult()).toBe(false);
    });

    it('runs photo recognition when asset id is present', async () => {
        const { aiFoodService, component, fixture } = await setupAiInputBarAsync();
        fixture.detectChanges();

        component.onPhotoSelected({ url: 'https://example.com/photo.jpg', assetId: 'asset-1' });

        expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledWith({ imageAssetId: 'asset-1' });
        expect(aiFoodService.calculateNutrition).toHaveBeenCalledWith({ items: VISION_ITEMS });
        expect(component.photoNutrition()).toEqual(NUTRITION);
    });
});
