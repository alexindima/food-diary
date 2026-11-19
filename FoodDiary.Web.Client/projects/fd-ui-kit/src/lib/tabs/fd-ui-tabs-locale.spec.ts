import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FdUiTabsComponent } from './fd-ui-tabs';

describe('FdUiTabsComponent scoped translation', () => {
    it('renders and updates tab names in its own locale without changing the global locale', async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiTabsComponent],
            providers: [provideTranslateTesting(), provideRouter([])],
        })
            .overrideComponent(FdUiTabsComponent, { add: { providers: provideTranslateTesting() } })
            .compileComponents();

        const global = TestBed.inject(TranslateService);
        global.setTranslation('en', { RECIPES: 'Recipes' });
        await firstValueFrom(global.use('en'));
        const fixture = TestBed.createComponent(FdUiTabsComponent);
        const local = fixture.debugElement.injector.get(TranslateService);
        expect(local).not.toBe(global);
        local.setTranslation('fr', { RECIPES: 'Recettes' });
        local.setTranslation('ru', { RECIPES: 'Рецепты' });
        await firstValueFrom(local.use('fr'));

        fixture.componentRef.setInput('tabs', [{ value: 'recipes', labelKey: 'RECIPES' }]);
        fixture.componentRef.setInput('selectedValue', 'recipes');
        fixture.detectChanges();
        const tab = (fixture.nativeElement as HTMLElement).querySelector('[role="tab"]');
        expect(tab?.textContent.trim()).toBe('Recettes');
        expect(tab?.getAttribute('aria-selected')).toBe('true');

        await firstValueFrom(local.use('ru'));
        fixture.detectChanges();
        expect(tab?.textContent.trim()).toBe('Рецепты');
        expect(global.getCurrentLang()).toBe('en');
    });
});
