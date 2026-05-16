import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { EntityCardComponent } from './entity-card.component';
import type { EntityCardNutrition } from './entity-card-lib/entity-card.types';

const NUTRITION: EntityCardNutrition = {
    proteins: 10,
    fats: 5,
    carbs: 20,
    fiber: 3,
    alcohol: 0,
};
const CALORIES = 123;

type EntityCardTestContext = {
    component: EntityCardComponent;
    fixture: ComponentFixture<EntityCardComponent>;
    translateService: TranslateService;
};

async function setupEntityCardAsync(): Promise<EntityCardTestContext> {
    await TestBed.configureTestingModule({
        imports: [EntityCardComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(EntityCardComponent);
    const component = fixture.componentInstance;
    const translateService = TestBed.inject(TranslateService);
    fixture.componentRef.setInput('imageAlt', 'Test image');
    fixture.componentRef.setInput('title', 'Test title');
    fixture.componentRef.setInput('nutrition', NUTRITION);
    fixture.componentRef.setInput('calories', CALORIES);

    return { component, fixture, translateService };
}

describe('EntityCardComponent preview state', () => {
    it('does not expose preview interaction when preview is disabled', async () => {
        const { component, fixture } = await setupEntityCardAsync();
        fixture.componentRef.setInput('imageUrl', 'https://example.com/photo.jpg');
        fixture.detectChanges();

        expect(component.hasPreviewImage()).toBe(false);
        expect(component.previewInteractionState()).toEqual({
            hint: null,
            role: null,
            tabIndex: null,
            ariaLabel: null,
        });
    });

    it('exposes preview interaction when image preview is available', async () => {
        const { component, fixture, translateService } = await setupEntityCardAsync();
        vi.spyOn(translateService, 'instant').mockReturnValue('Open preview');
        fixture.componentRef.setInput('previewable', true);
        fixture.componentRef.setInput('imageUrl', 'https://example.com/photo.jpg');
        fixture.detectChanges();

        expect(component.hasPreviewImage()).toBe(true);
        expect(component.previewInteractionState()).toEqual({
            hint: 'Open preview',
            role: 'button',
            tabIndex: '0',
            ariaLabel: 'Open preview',
        });
    });
});

describe('EntityCardComponent computed view model', () => {
    it('limits collage images and normalizes quality score', async () => {
        const { component, fixture } = await setupEntityCardAsync();
        fixture.componentRef.setInput('collageImages', [
            { url: '1.jpg', alt: '1' },
            { url: '2.jpg', alt: '2' },
            { url: '3.jpg', alt: '3' },
            { url: '4.jpg', alt: '4' },
            { url: '5.jpg', alt: '5' },
        ]);
        fixture.componentRef.setInput('quality', { score: 150, grade: 'green' });
        fixture.detectChanges();

        expect(component.collageState()).toEqual({
            images: [
                { url: '1.jpg', alt: '1' },
                { url: '2.jpg', alt: '2' },
                { url: '3.jpg', alt: '3' },
                { url: '4.jpg', alt: '4' },
            ],
            count: 4,
            hasImages: true,
        });
        expect(component.normalizedQuality()).toEqual({
            score: 100,
            grade: 'green',
            hintKey: 'QUALITY.GREEN',
        });
    });
});

describe('EntityCardComponent events', () => {
    it('forwards user actions through outputs', async () => {
        const { component, fixture } = await setupEntityCardAsync();
        const openSpy = vi.fn();
        const previewSpy = vi.fn();
        const favoriteSpy = vi.fn();
        const actionSpy = vi.fn();
        component.open.subscribe(openSpy);
        component.preview.subscribe(previewSpy);
        component.favoriteToggle.subscribe(favoriteSpy);
        component.action.subscribe(actionSpy);
        fixture.detectChanges();

        component.handleOpen();
        component.handlePreview();
        component.handleFavoriteToggle();
        component.handleAction();

        expect(openSpy).toHaveBeenCalledOnce();
        expect(previewSpy).toHaveBeenCalledOnce();
        expect(favoriteSpy).toHaveBeenCalledOnce();
        expect(actionSpy).toHaveBeenCalledOnce();
    });
});
