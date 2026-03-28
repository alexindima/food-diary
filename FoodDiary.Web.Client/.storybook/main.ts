import type { StorybookConfig } from '@storybook/angular';

const config: StorybookConfig = {
    stories: ['../projects/fd-ui-kit/src/**/*.stories.@(ts|mdx)'],
    framework: {
        name: '@storybook/angular',
        options: {},
    },
    staticDirs: ['../assets'],
};

export default config;
