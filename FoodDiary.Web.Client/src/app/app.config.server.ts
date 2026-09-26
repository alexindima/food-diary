import { type ApplicationConfig, mergeApplicationConfig } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';

import english from '../../assets/i18n/en.json';
import russian from '../../assets/i18n/ru.json';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { PUBLIC_RECIPE_API_URL } from './features/recipes/api/public-recipe-api.token';
import { SERVER_TRANSLATIONS } from './shared/i18n/server-translations.token';

const publicApiOrigin = process.env['SSR_API_ORIGIN'] ?? 'http://localhost:5300';

const serverConfig: ApplicationConfig = {
    providers: [
        provideServerRendering(withRoutes(serverRoutes)),
        { provide: SERVER_TRANSLATIONS, useValue: { en: english, ru: russian } },
        {
            provide: PUBLIC_RECIPE_API_URL,
            useValue: `${publicApiOrigin}/api/v1/recipes/public`,
        },
    ],
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
