import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import type { Preview } from '@storybook/angular';
import { applicationConfig } from '@storybook/angular';

const preview: Preview = {
    decorators: [
        applicationConfig({
            providers: [provideAnimationsAsync()],
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
