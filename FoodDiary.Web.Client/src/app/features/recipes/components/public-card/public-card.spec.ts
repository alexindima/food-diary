import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { publicRecipeFixture } from '../../lib/public-recipe.test-helper';
import { PublicRecipeCardComponent } from './public-card';

describe('PublicRecipeCardComponent', () => {
    function createCard(): ReturnType<typeof TestBed.createComponent<PublicRecipeCardComponent>> {
        TestBed.configureTestingModule({ providers: [provideRouter([]), provideTranslateTesting()] });
        const fixture = TestBed.createComponent(PublicRecipeCardComponent);
        fixture.componentRef.setInput('recipe', publicRecipeFixture());
        fixture.detectChanges();
        return fixture;
    }

    it('keeps the favorite button separate from the recipe link and disables duplicate clicks', () => {
        const fixture = createCard();
        const requested = vi.fn();
        fixture.componentInstance.favoriteRequested.subscribe(requested);
        const element = fixture.nativeElement as HTMLElement;
        const button = element.querySelector<HTMLButtonElement>('.catalog-favorite button');
        expect(button?.closest('a')).toBeNull();
        button?.click();
        expect(requested).toHaveBeenCalledTimes(1);
        fixture.componentRef.setInput('busy', true);
        fixture.componentRef.setInput('saved', true);
        fixture.detectChanges();
        expect(button?.disabled).toBe(true);
        expect(button?.getAttribute('aria-label')).toBe('PUBLIC_RECIPES.UNSAVE');
    });

    it('shows unknown nutrition as missing data while preserving the partial-calculation explanation', () => {
        const fixture = createCard();
        fixture.componentRef.setInput('recipe', { ...publicRecipeFixture(), totalCalories: null });
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('.catalog-calories')?.textContent).toContain('PUBLIC_RECIPES.NO_NUTRITION');
        expect(element.querySelector('[aria-label="PUBLIC_RECIPES.PARTIAL"]')).not.toBeNull();
    });

    it('loads only the prioritized cover eagerly', () => {
        const fixture = createCard();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('img')?.getAttribute('loading')).toBe('lazy');
        fixture.componentRef.setInput('priority', true);
        fixture.detectChanges();
        expect(element.querySelector('img')?.getAttribute('loading')).toBe('eager');
    });
    it('replaces a failed cover and retries when the image URL changes', () => {
        const fixture = createCard();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('.catalog-cover--loading')).not.toBeNull();
        element.querySelector('img')?.dispatchEvent(new Event('error'));
        fixture.detectChanges();
        expect(element.querySelector('img')).toBeNull();
        expect(element.querySelector('.catalog-placeholder')).not.toBeNull();
        fixture.componentRef.setInput('recipe', { ...publicRecipeFixture(), imageUrl: 'https://test/new.jpg' });
        fixture.detectChanges();
        expect(element.querySelector('.catalog-cover--loading')).not.toBeNull();
        element.querySelector('img')?.dispatchEvent(new Event('load'));
        fixture.detectChanges();
        expect(element.querySelector('.catalog-cover--loading')).toBeNull();
        expect(element.querySelector('.catalog-image--loaded')).not.toBeNull();
    });
});
