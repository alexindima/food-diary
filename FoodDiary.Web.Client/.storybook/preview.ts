import { provideHttpClient } from '@angular/common/http';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { applicationConfig, type Preview } from '@storybook/angular-vite';

const preview: Preview = {
    decorators: [
        applicationConfig({
            providers: [
                provideHttpClient(),
                provideTranslateService({
                    lang: 'en',
                    fallbackLang: 'en',
                    loader: provideTranslateHttpLoader({ prefix: './i18n/', suffix: '.json' }),
                }),
            ],
        }),
    ],
    parameters: {
        controls: {
            matchers: {
                color: /(background|color)$/i,
                date: /date$/i,
            },
        },
        docs: {
            toc: true,
        },
    },
};

export default preview;
