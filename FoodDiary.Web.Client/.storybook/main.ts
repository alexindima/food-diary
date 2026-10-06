import type { StorybookConfig } from '@storybook/angular-vite';
import { mergeConfig } from 'vite';

const config: StorybookConfig = {
    stories: ['../projects/fd-ui-kit/src/**/*.stories.@(ts|mdx)'],
    framework: {
        name: '@storybook/angular-vite',
        options: {},
    },
    staticDirs: ['../assets'],
    viteFinal(viteConfig) {
        return mergeConfig(viteConfig, {
            resolve: { tsconfigPaths: true },
        });
    },
};

export default config;
