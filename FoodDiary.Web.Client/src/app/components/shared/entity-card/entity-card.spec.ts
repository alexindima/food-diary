import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { EntityCardComponent } from './entity-card';
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
        imports: [EntityCardComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(EntityCardComponent);
    const component = fixture.componentInstance;
    const translateService = TestBed.inject(TranslateService);
    fixture.componentRef.setInput('imageAlt', 'Test image');
    fixture.componentRef.setInput('title', 'Test title');
    fixture.componentRef.setInput('nutrition', NUTRITION);
    fixture.componentRef.setInput('calories', CALORIES);
    fixture.componentRef.setInput('favoriteAriaLabel', 'Toggle favorite');

    return { component, fixture, translateService };
}

describe('EntityCardComponent preview state', () => {
    it('does not expose preview interaction when preview is disabled', async () => {
        const { component, fixture } = await setupEntityCardAsync();
        fixture.componentRef.setInput('imageUrl', 'https://example.com/photo.jpg');
        fixture.detectChanges();

        expect(component['hasPreviewImage']()).toBe(false);
        expect(component['previewInteractionState']()).toEqual({
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

        expect(component['hasPreviewImage']()).toBe(true);
        expect(component['previewInteractionState']()).toEqual({
            hint: 'Open preview',
            role: 'button',
            tabIndex: '0',
            ariaLabel: 'Open preview',
        });
    });

    it('treats whitespace image URL as missing image', async () => {
        const { component, fixture } = await setupEntityCardAsync();
        fixture.componentRef.setInput('previewable', true);
        fixture.componentRef.setInput('imageUrl', '   ');
        fixture.detectChanges();

        const el = fixture.nativeElement as HTMLElement;
        expect(component['normalizedImageUrl']()).toBeNull();
        expect(component['hasPreviewImage']()).toBe(false);
        expect(el.querySelector('.entity-card__thumb img')).toBeNull();
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

        expect(component['collageState']()).toEqual({
            images: [
                { url: '1.jpg', alt: '1' },
                { url: '2.jpg', alt: '2' },
                { url: '3.jpg', alt: '3' },
                { url: '4.jpg', alt: '4' },
            ],
            count: 4,
            hasImages: true,
        });
        expect(component['normalizedQuality']()).toEqual({
            score: 100,
            grade: 'green',
            hintKey: 'QUALITY.GREEN',
        });
    });
});

describe('EntityCardComponent badges', () => {
    it('renders ownership icon as media overlay', async () => {
        const { fixture } = await setupEntityCardAsync();
        fixture.componentRef.setInput('ownershipIcon', 'groups');
        fixture.detectChanges();

        const el = fixture.nativeElement as HTMLElement;
        const mediaShell = el.querySelector('.entity-card__media-shell');
        const badge = mediaShell?.querySelector('.entity-card__ownership-badge');

        expect(badge).not.toBeNull();
        expect(badge?.querySelector('.entity-card__ownership-badge-icon')).not.toBeNull();
        expect(el.querySelector('.entity-card__name-row .entity-card__ownership-badge')).toBeNull();
    });

    it('does not render ownership overlay without ownership icon', async () => {
        const { fixture } = await setupEntityCardAsync();
        fixture.detectChanges();

        const el = fixture.nativeElement as HTMLElement;
        expect(el.querySelector('.entity-card__ownership-badge')).toBeNull();
    });

    it('renders favorite button with accessible label', async () => {
        const { fixture } = await setupEntityCardAsync();
        fixture.componentRef.setInput('showFavorite', true);
        fixture.componentRef.setInput('favoriteAriaLabel', 'Remove from favorites');
        fixture.detectChanges();

        const el = fixture.nativeElement as HTMLElement;
        const favoriteButton = el.querySelector('.entity-card__favorite-button');
        expect(favoriteButton?.getAttribute('aria-label')).toBe('Remove from favorites');
    });
});

describe('EntityCardComponent events', () => {
    it('forwards user actions through outputs', async () => {
        const { component, fixture } = await setupEntityCardAsync();
        const openSpy = vi.fn();
        const previewSpy = vi.fn();
        const favoriteSpy = vi.fn();
        const actionSpy = vi.fn();
        component['open'].subscribe(openSpy);
        component['preview'].subscribe(previewSpy);
        component['favoriteToggle'].subscribe(favoriteSpy);
        component['action'].subscribe(actionSpy);
        fixture.detectChanges();

        component['openCard']();
        component['previewCardImage']();
        component['toggleFavorite']();
        component['emitCardAction']();

        expect(openSpy).toHaveBeenCalledOnce();
        expect(previewSpy).toHaveBeenCalledOnce();
        expect(favoriteSpy).toHaveBeenCalledOnce();
        expect(actionSpy).toHaveBeenCalledOnce();
    });

    it('opens from keyboard only when the card itself handles the event', async () => {
        const { component, fixture } = await setupEntityCardAsync();
        const openSpy = vi.fn();
        const preventDefaultSpy = vi.fn();
        const el = fixture.nativeElement as HTMLElement;
        const cardElement = el.querySelector('fd-media-card') as HTMLElement;
        const childElement = document.createElement('button');
        component['open'].subscribe(openSpy);
        fixture.detectChanges();

        component['openCardFromKeyboard']({
            currentTarget: cardElement,
            target: childElement,
            preventDefault: preventDefaultSpy,
        } as unknown as KeyboardEvent);

        expect(openSpy).not.toHaveBeenCalled();
        expect(preventDefaultSpy).not.toHaveBeenCalled();

        component['openCardFromKeyboard']({
            currentTarget: cardElement,
            target: cardElement,
            preventDefault: preventDefaultSpy,
        } as unknown as KeyboardEvent);

        expect(openSpy).toHaveBeenCalledOnce();
        expect(preventDefaultSpy).toHaveBeenCalledOnce();
    });

    it('stops favorite keyboard events from reaching the card', async () => {
        const { component, fixture } = await setupEntityCardAsync();
        const stopPropagationSpy = vi.fn();
        fixture.detectChanges();

        component['stopCardKeyboardEvent']({ stopPropagation: stopPropagationSpy } as unknown as KeyboardEvent);

        expect(stopPropagationSpy).toHaveBeenCalledOnce();
    });
});
