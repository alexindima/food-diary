import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import englishCore from '../../../../../../../assets/i18n/en/core.json';
import russianCore from '../../../../../../../assets/i18n/ru/core.json';
import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { RecipePublicationNoticeComponent } from './recipe-publication-notice';

type NoticeStatus = ReturnType<RecipePublicationNoticeComponent['status']>;

async function mountNoticeAsync(status: NoticeStatus): Promise<{
    fixture: ComponentFixture<RecipePublicationNoticeComponent>;
    host: HTMLElement;
    translate: TranslateService;
}> {
    TestBed.configureTestingModule({
        imports: [RecipePublicationNoticeComponent],
        providers: provideTranslateTesting(),
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { RECIPE_PUBLICATION: englishCore.RECIPE_PUBLICATION });
    translate.setTranslation('ru', { RECIPE_PUBLICATION: russianCore.RECIPE_PUBLICATION });
    await firstValueFrom(translate.use('en'));
    const fixture = TestBed.createComponent(RecipePublicationNoticeComponent);
    fixture.componentRef.setInput('status', status);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    return { fixture, host, translate };
}

describe('Recipe publication notice', () => {
    it.each([
        { status: 'Pending', en: 'Under moderation', ru: 'На модерации' },
        { status: 'NeedsReview', en: 'Awaiting moderator review', ru: 'Ожидает решения модератора' },
    ] as const)('updates the $status status and refresh action when the active language changes', async ({ status, en, ru }) => {
        const { fixture, host, translate } = await mountNoticeAsync(status);
        expect(host.querySelector('[role="status"]')?.textContent.trim()).toBe(en);
        expect(host.querySelector('button')?.textContent.trim()).toBe('Refresh status');

        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();

        expect(host.querySelector('[role="status"]')?.textContent.trim()).toBe(ru);
        expect(host.querySelector('button')?.textContent.trim()).toBe('Обновить статус');
    });

    it('announces image guidance in the current language and displays a moderator note verbatim', async () => {
        const { fixture, host, translate } = await mountNoticeAsync('ChangesRequested');
        fixture.componentRef.setInput('reason', 'ReplaceImage');
        fixture.detectChanges();

        expect(host.querySelector('[role="status"]')?.textContent).toContain('Changes requested');
        expect(host.querySelector('[role="status"] p')?.textContent.trim()).toBe(
            'Upload the photo to FoodDiary and submit the recipe again.',
        );

        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();

        expect(host.querySelector('[role="status"]')?.textContent).toContain('Требуются правки');
        expect(host.querySelector('[role="status"] p')?.textContent.trim()).toBe(
            'Загрузите фото в FoodDiary и отправьте рецепт на проверку ещё раз.',
        );

        const note = 'Please specify the oven temperature.';
        fixture.componentRef.setInput('reason', note);
        fixture.detectChanges();
        expect(host.querySelector('[role="status"] p')?.textContent.trim()).toBe(note);
    });

    it('leaves a recipe without publication status free of an announcement or refresh action', async () => {
        const { host } = await mountNoticeAsync(null);
        expect(host.querySelector('[role="status"]')).toBeNull();
        expect(host.querySelector('button')).toBeNull();
    });

    it('offers a native retry after a refresh fails and removes it when the recipe is published', async () => {
        const { fixture, host } = await mountNoticeAsync('CheckDelayed');
        const refreshRequested = vi.fn();
        fixture.componentInstance.refreshRequested.subscribe(refreshRequested);
        expect(host.querySelector('[role="status"]')?.textContent.trim()).toBe('Moderation delayed');
        const button = host.querySelector('button');
        if (button === null) {
            throw new Error('Expected a native refresh button while moderation is delayed');
        }
        expect(button.type).toBe('button');
        button.click();
        expect(refreshRequested).toHaveBeenCalledOnce();

        fixture.componentRef.setInput('loading', true);
        fixture.detectChanges();
        expect(button.disabled).toBe(true);
        button.click();
        expect(refreshRequested).toHaveBeenCalledOnce();

        fixture.componentRef.setInput('loading', false);
        fixture.componentRef.setInput('refreshFailed', true);
        fixture.detectChanges();
        expect(host.querySelector('[role="status"] p')?.textContent.trim()).toBe('Could not refresh the status. Try again.');
        expect(button.disabled).toBe(false);
        button.click();
        expect(refreshRequested).toHaveBeenCalledTimes(2);

        fixture.componentRef.setInput('refreshFailed', false);
        fixture.componentRef.setInput('status', 'Published');
        fixture.detectChanges();
        expect(host.querySelector('[role="status"]')?.textContent.trim()).toBe('Published');
        expect(host.querySelector('button')).toBeNull();
    });
});
