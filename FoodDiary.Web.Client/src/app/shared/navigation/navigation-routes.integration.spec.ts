import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { NavigationService } from '../../services/navigation.service';
import { LocalizationService } from '../i18n/localization.service';

@Component({
    template: '',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class NavigationDestinationComponent {}

const ENTRY_ID = '6078bb84-623a-4ab7-ad78-170531fea7a4';

type Destination = {
    url: string;
    openAsync: (navigation: NavigationService) => Promise<void>;
};

const destinations: readonly Destination[] = [
    { url: '/change-password-required', openAsync: async navigation => navigation.navigateToRequiredPasswordChangeAsync() },
    { url: '/products', openAsync: async navigation => navigation.navigateToProductListAsync() },
    { url: '/products/add', openAsync: async navigation => navigation.navigateToProductAddAsync() },
    { url: `/meals/${ENTRY_ID}/edit`, openAsync: async navigation => navigation.navigateToMealEditAsync(ENTRY_ID) },
    { url: '/recipes', openAsync: async navigation => navigation.navigateToRecipeListAsync() },
    { url: '/recipes/add', openAsync: async navigation => navigation.navigateToRecipeAddAsync() },
    { url: `/recipes/${ENTRY_ID}/edit`, openAsync: async navigation => navigation.navigateToRecipeEditAsync(ENTRY_ID) },
    { url: '/fasting', openAsync: async navigation => navigation.navigateToFastingAsync() },
    { url: '/dietologist', openAsync: async navigation => navigation.navigateToDietologistAsync() },
    { url: '/premium', openAsync: async navigation => navigation.navigateToPremiumAccessAsync() },
    { url: '/weight-history', openAsync: async navigation => navigation.navigateToWeightHistoryAsync() },
];

describe('navigation into app screens', () => {
    let navigation: NavigationService;
    let router: Router;

    beforeEach(async () => {
        TestBed.configureTestingModule({
            providers: [
                provideRouter([{ path: '**', component: NavigationDestinationComponent }]),
                { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } },
            ],
        });
        await RouterTestingHarness.create('/dashboard');
        router = TestBed.inject(Router);
        navigation = TestBed.inject(NavigationService);
    });

    it.each(destinations)('navigates to $url', async ({ url, openAsync }) => {
        await openAsync(navigation);

        expect(router.url).toBe(url);
    });

    it('opens manual meal entry from the dashboard without supplying a meal type', async () => {
        await expect(navigation.navigateToMealAddAsync(undefined, { manual: true })).resolves.toBe(true);

        expect(router.url).toBe('/meals/add?manual=1');
        expect(router.routerState.snapshot.root.queryParamMap.has('mealType')).toBe(false);
    });
});
