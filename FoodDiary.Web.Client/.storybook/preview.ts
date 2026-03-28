import type { Preview } from '@storybook/angular';
import { applicationConfig } from '@storybook/angular';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideNativeDateAdapter } from '@angular/material/core';

const preview: Preview = {
    decorators: [
        applicationConfig({
            providers: [provideAnimationsAsync(), provideNativeDateAdapter()],
        }),
    ],
    parameters: {
        controls: {
            matchers: {
                color: /(background|color)$/i,
                date: /Date$/i,
            },
        },
        docs: {
            toc: true,
        },
    },
};

export default preview;
