import type { Meta, StoryObj } from '@storybook/angular';

import { FdUiPaginationComponent } from './fd-ui-pagination.component';

const meta: Meta<FdUiPaginationComponent> = {
    title: 'Components/Pagination',
    component: FdUiPaginationComponent,
    tags: ['autodocs'],
    argTypes: {
        length: { control: 'number', description: 'Total number of items' },
        pageSize: { control: 'number', description: 'Items per page' },
        pageIndex: { control: 'number', description: 'Current page (0-based)' },
    },
};

export default meta;
type Story = StoryObj<FdUiPaginationComponent>;

export const Default: Story = {
    args: {
        length: 100,
        pageSize: 10,
        pageIndex: 0,
    },
};

export const MiddlePage: Story = {
    args: {
        length: 100,
        pageSize: 10,
        pageIndex: 5,
    },
};

export const FewItems: Story = {
    args: {
        length: 25,
        pageSize: 10,
        pageIndex: 0,
    },
};

export const SinglePage: Story = {
    args: {
        length: 5,
        pageSize: 10,
        pageIndex: 0,
    },
};

export const LargeDataset: Story = {
    args: {
        length: 1000,
        pageSize: 20,
        pageIndex: 0,
    },
};
