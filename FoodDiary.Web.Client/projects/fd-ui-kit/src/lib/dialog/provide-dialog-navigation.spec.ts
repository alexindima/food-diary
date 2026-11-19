import { APP_BASE_HREF, Location, LocationStrategy, PlatformLocation } from '@angular/common';
import { MockPlatformLocation } from '@angular/common/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { FD_UI_DIALOG_HISTORY } from './fd-ui-dialog-history';
import { provideFdUiDialogNavigation } from './provide-dialog-navigation';

function setupNavigation(injectedBaseHref: string | null = '/base/'): {
    location: Location;
    platform: MockPlatformLocation;
    strategy: LocationStrategy;
    handlePopState: ReturnType<typeof vi.fn>;
} {
    const platform = new MockPlatformLocation({ startUrl: 'https://fooddiary.test/base/current', appBaseHref: '/base/' });
    const handlePopState = vi.fn().mockReturnValue(true);
    TestBed.configureTestingModule({
        providers: [
            provideFdUiDialogNavigation(),
            { provide: PlatformLocation, useValue: platform },
            ...(injectedBaseHref === null ? [] : [{ provide: APP_BASE_HREF, useValue: injectedBaseHref }]),
            { provide: FD_UI_DIALOG_HISTORY, useValue: { handlePopState, recordPagePush: vi.fn() } },
        ],
    });
    return { platform, handlePopState, location: TestBed.inject(Location), strategy: TestBed.inject(LocationStrategy) };
}

describe('dialog navigation provider', () => {
    it('consumes dismissal before Angular Location can notify Router or CDK', () => {
        const { location, platform, handlePopState } = setupNavigation();
        const navigation = vi.fn();
        location.subscribe(navigation);
        platform.pushState({}, '', '/base/current');
        platform.back();

        expect(handlePopState).toHaveBeenCalledOnce();
        expect(navigation).not.toHaveBeenCalled();
    });

    it('preserves real navigation and its state for existing Angular subscribers', () => {
        const { location, platform, handlePopState } = setupNavigation();
        handlePopState.mockReturnValue(false);
        const navigation = vi.fn();
        location.subscribe(navigation);
        platform.replaceState({ navigationId: 1 }, '', '/base/current');
        platform.pushState({ navigationId: 2 }, '', '/base/next');
        platform.back();

        expect(navigation).toHaveBeenCalledWith(expect.objectContaining({ url: '/current', state: { navigationId: 1 }, pop: true }));
    });

    it('retains path routing, base href, query strings, and replacement state', () => {
        const { location, platform, strategy } = setupNavigation();
        expect(strategy.prepareExternalUrl('/next')).toBe('/base/next');
        location.go('/next', 'page=2', { editor: 'draft' });
        expect(platform.pathname).toBe('/base/next');
        expect(platform.search).toBe('?page=2');
        expect(location.getState()).toEqual({ editor: 'draft' });
        location.replaceState('/next', 'page=3', { editor: 'updated' });
        expect(platform.search).toBe('?page=3');
        expect(location.getState()).toEqual({ editor: 'updated' });
    });

    it('uses the browser base href when APP_BASE_HREF is not supplied', () => {
        const { location, platform, strategy } = setupNavigation(null);

        expect(location.path()).toBe('/current');
        expect(strategy.prepareExternalUrl('/archive')).toBe('/base/archive');
        location.go('/archive', 'page=2');
        expect(platform.pathname).toBe('/base/archive');
        expect(platform.search).toBe('?page=2');
    });
});
