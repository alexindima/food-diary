import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { FavoritesSectionComponent } from './favorites-section.component';

const FAVORITES_COUNT = 3;

describe('FavoritesSectionComponent', () => {
    it('renders closed header and emits toggle', () => {
        const fixture = createComponent({ isOpen: false });
        const toggle = vi.fn();
        fixture.componentInstance.toggleRequested.subscribe(toggle);

        getHost(fixture).querySelector<HTMLButtonElement>('.fd-favorites-section__header')?.click();

        expect(getHost(fixture).textContent).toContain(`Favorites (${FAVORITES_COUNT})`);
        expect(getHost(fixture).querySelector('.fd-favorites-section__body')).toBeNull();
        expect(toggle).toHaveBeenCalledOnce();
    });

    it('emits load more only when visible and not loading', () => {
        const fixture = createComponent({ isOpen: true, showLoadMore: true, isLoadingMore: false });
        const loadMore = vi.fn();
        fixture.componentInstance.loadMore.subscribe(loadMore);

        getHost(fixture).querySelectorAll<HTMLButtonElement>('button')[1].click();

        expect(loadMore).toHaveBeenCalledOnce();
    });

    it('does not emit load more while loading', () => {
        const fixture = createComponent({ isOpen: true, showLoadMore: true, isLoadingMore: true });
        const loadMore = vi.fn();
        fixture.componentInstance.loadMore.subscribe(loadMore);

        fixture.componentInstance.onLoadMore();

        expect(loadMore).not.toHaveBeenCalled();
    });
});

function createComponent(inputs: {
    isOpen: boolean;
    showLoadMore?: boolean;
    isLoadingMore?: boolean;
}): ComponentFixture<FavoritesSectionComponent> {
    TestBed.configureTestingModule({
        imports: [FavoritesSectionComponent],
    });

    const fixture = TestBed.createComponent(FavoritesSectionComponent);
    fixture.componentRef.setInput('title', 'Favorites');
    fixture.componentRef.setInput('count', FAVORITES_COUNT);
    fixture.componentRef.setInput('isOpen', inputs.isOpen);
    fixture.componentRef.setInput('showLoadMore', inputs.showLoadMore ?? false);
    fixture.componentRef.setInput('isLoadingMore', inputs.isLoadingMore ?? false);
    fixture.componentRef.setInput('loadMoreLabel', 'Load more');
    fixture.detectChanges();
    return fixture;
}

function getHost(fixture: ComponentFixture<FavoritesSectionComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}
