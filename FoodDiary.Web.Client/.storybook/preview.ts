import type { Preview } from '@storybook/angular';

const preview: Preview = {
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
