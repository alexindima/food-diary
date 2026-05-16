export const EXPLORE_PAGE_SIZE = 20;

export type ExploreSort = 'newest' | 'popular';

export type ExploreSortAction = {
    value: ExploreSort;
    labelKey: string;
    variant: 'primary' | 'outline';
};
