import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it } from 'vitest';

import { environment } from '../../../environments/environment';
import { AdminTranslationLoader } from './admin-translation.loader';

describe('AdminTranslationLoader', () => {
    it.each([
        ['ru-RU', 'ru'],
        ['en-US', 'en'],
        ['fr', 'en'],
    ])('loads the %s dictionary under its build version', async (language, normalizedLanguage) => {
        const { loader, http } = setup();
        const result = firstValueFrom(loader.getTranslation(language));
        const request = http.expectOne(`./assets/i18n/${normalizedLanguage}/core.json?v=${encodeURIComponent(environment.buildVersion)}`);

        request.flush({ ADMIN_COMMON: { SAVE: 'Save from current build' } });

        await expect(result).resolves.toEqual({ ADMIN_COMMON: { SAVE: 'Save from current build' } });
        http.verify();
    });

    it('keeps language dictionaries separate within the same build', async () => {
        const { loader, http } = setup();
        const english = firstValueFrom(loader.getTranslation('en'));
        const russian = firstValueFrom(loader.getTranslation('ru'));
        const version = encodeURIComponent(environment.buildVersion);
        http.expectOne(`./assets/i18n/en/core.json?v=${version}`).flush({ SAVE: 'Save' });
        http.expectOne(`./assets/i18n/ru/core.json?v=${version}`).flush({ SAVE: 'Сохранить' });

        await expect(english).resolves.toEqual({ SAVE: 'Save' });
        await expect(russian).resolves.toEqual({ SAVE: 'Сохранить' });
        http.verify();
    });

    it('allows another request after a temporary loading failure', async () => {
        const { loader, http } = setup();
        const url = `./assets/i18n/en/core.json?v=${encodeURIComponent(environment.buildVersion)}`;
        const first = firstValueFrom(loader.getTranslation('en'));
        http.expectOne(url).flush('temporarily unavailable', { status: 503, statusText: 'Service Unavailable' });
        await expect(first).resolves.toEqual({});

        const retry = firstValueFrom(loader.getTranslation('en'));
        http.expectOne(url).flush({ SAVE: 'Recovered' });

        await expect(retry).resolves.toEqual({ SAVE: 'Recovered' });
        http.verify();
    });
});

function setup(): { loader: AdminTranslationLoader; http: HttpTestingController } {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    return { loader: TestBed.inject(AdminTranslationLoader), http: TestBed.inject(HttpTestingController) };
}
