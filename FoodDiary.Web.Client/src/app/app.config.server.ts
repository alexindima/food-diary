import { type ApplicationConfig, CSP_NONCE, inject, mergeApplicationConfig, REQUEST_CONTEXT } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';

import englishApp from '../../assets/i18n/en/app.json';
import englishCore from '../../assets/i18n/en/core.json';
import englishLanding from '../../assets/i18n/en/landing.json';
import englishPrivacy from '../../assets/i18n/en/privacy.json';
import englishSeo from '../../assets/i18n/en/seo.json';
import russianApp from '../../assets/i18n/ru/app.json';
import russianCore from '../../assets/i18n/ru/core.json';
import russianLanding from '../../assets/i18n/ru/landing.json';
import russianPrivacy from '../../assets/i18n/ru/privacy.json';
import russianSeo from '../../assets/i18n/ru/seo.json';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { PUBLIC_RECIPE_API_URL } from './features/recipes/api/public-recipe-api.token';
import { SERVER_TRANSLATIONS } from './shared/i18n/server-translations.token';
import { getStringProperty } from './shared/lib/unknown-value.utils';

const publicApiOrigin = process.env['SSR_API_ORIGIN'] ?? 'http://localhost:5300';

const serverConfig: ApplicationConfig = {
    providers: [
        provideServerRendering(withRoutes(serverRoutes)),
        { provide: CSP_NONCE, useFactory: () => getStringProperty(inject(REQUEST_CONTEXT), 'cspNonce') ?? null },
        {
            provide: SERVER_TRANSLATIONS,
            useValue: {
                en: { core: englishCore, landing: englishLanding, seo: englishSeo, privacy: englishPrivacy, app: englishApp },
                ru: { core: russianCore, landing: russianLanding, seo: russianSeo, privacy: russianPrivacy, app: russianApp },
            },
        },
        {
            provide: PUBLIC_RECIPE_API_URL,
            useValue: `${publicApiOrigin}/api/v1/recipes/public`,
        },
    ],
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
