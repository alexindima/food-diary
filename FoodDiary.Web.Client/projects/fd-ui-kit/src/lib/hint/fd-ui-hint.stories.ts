import type { Meta, StoryObj } from '@storybook/angular';
import { moduleMetadata } from '@storybook/angular';
import { FdUiHintDirective } from './fd-ui-hint.directive';
import { FdUiButtonComponent } from '../button/fd-ui-button.component';

const meta: Meta = {
    title: 'Components/Hint',
    decorators: [
        moduleMetadata({
            imports: [FdUiHintDirective, FdUiButtonComponent],
        }),
    ],
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj;

export const Default: Story = {
    render: () => ({
        template: `
            <div style="padding: 60px; display: flex; justify-content: center;">
                <fd-ui-button variant="secondary" fill="outline" fdUiHint="This is a helpful tooltip">Hover me</fd-ui-button>
            </div>
        `,
    }),
};

export const Top: Story = {
    render: () => ({
        template: `
            <div style="padding: 60px; display: flex; justify-content: center;">
                <fd-ui-button variant="secondary" fill="outline" fdUiHint="Tooltip on top" fdUiHintPosition="top">Top</fd-ui-button>
            </div>
        `,
    }),
};

export const Bottom: Story = {
    render: () => ({
        template: `
            <div style="padding: 60px; display: flex; justify-content: center;">
                <fd-ui-button variant="secondary" fill="outline" fdUiHint="Tooltip on bottom" fdUiHintPosition="bottom">Bottom</fd-ui-button>
            </div>
        `,
    }),
};

export const Left: Story = {
    render: () => ({
        template: `
            <div style="padding: 60px; display: flex; justify-content: center;">
                <fd-ui-button variant="secondary" fill="outline" fdUiHint="Tooltip on left" fdUiHintPosition="left">Left</fd-ui-button>
            </div>
        `,
    }),
};

export const Right: Story = {
    render: () => ({
        template: `
            <div style="padding: 60px; display: flex; justify-content: center;">
                <fd-ui-button variant="secondary" fill="outline" fdUiHint="Tooltip on right" fdUiHintPosition="right">Right</fd-ui-button>
            </div>
        `,
    }),
};

export const WithHtml: Story = {
    render: () => ({
        template: `
            <div style="padding: 60px; display: flex; justify-content: center;">
                <fd-ui-button variant="secondary" fill="outline" fdUiHint="<strong>Bold</strong> and <em>italic</em> tooltip" [fdUiHintHtml]="true">HTML Hint</fd-ui-button>
            </div>
        `,
    }),
};

export const AllPositions: Story = {
    render: () => ({
        template: `
            <div style="padding: 80px; display: flex; gap: 24px; justify-content: center;">
                <fd-ui-button variant="secondary" fill="outline" fdUiHint="Top tooltip" fdUiHintPosition="top">Top</fd-ui-button>
                <fd-ui-button variant="secondary" fill="outline" fdUiHint="Bottom tooltip" fdUiHintPosition="bottom">Bottom</fd-ui-button>
                <fd-ui-button variant="secondary" fill="outline" fdUiHint="Left tooltip" fdUiHintPosition="left">Left</fd-ui-button>
                <fd-ui-button variant="secondary" fill="outline" fdUiHint="Right tooltip" fdUiHintPosition="right">Right</fd-ui-button>
            </div>
        `,
    }),
};
