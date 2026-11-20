import { registerLocaleData } from '@angular/common';
import { type HttpInterceptorFn, HttpResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import localeRu from '@angular/common/locales/ru';
import { signal, type WritableSignal } from '@angular/core';
import { DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, type Observable, of, Subject } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import { publicSiteLocaleGuard } from '../../shared/i18n/public-site-locale.guard';
import { BrowserWindowService } from '../../shared/platform/browser-window.service';
import { PublicAuthDialogService } from './lib/public-auth-dialog.service';
import { MainComponent } from './pages/landing/main';

const products = {
    YOGURT: { NAME: 'Greek yogurt' },
    GRANOLA: { NAME: 'Oat granola' },
};
const recipes = {
    BOWL: { NAME: 'Breakfast bowl' },
    SALAD: { NAME: 'Chicken salad' },
};
const yogurtAdd = 'button[aria-label="PRODUCT_CARD.ADD_TO_MEAL"]';
const saladAdd = 'fd-recipe-card:last-child button[aria-label="RECIPE_CARD.ADD_TO_MEAL"]';
const saveDraft = 'button[aria-label="QUICK_MEAL.SAVE_ARIA"]';
registerLocaleData(localeRu);

type LandingFixture = {
    authenticated: WritableSignal<boolean>;
    closed: Subject<void>;
    dialog: Mock<PublicAuthDialogService['openAsync']>;
    harness: RouterTestingHarness;
    root: HTMLElement;
    translate: TranslateService;
};

const publicTranslationAssets: HttpInterceptorFn = (request, next) =>
    request.method === 'GET' && request.url.startsWith('./assets/i18n/') ? of(new HttpResponse({ body: {} })) : next(request);

describe('Public landing meal demo', () => {
    it('adds a product and a recipe to a cleared sample without persisting it', async () => {
        const { harness, root } = await renderLandingAsync('/');
        const draft = requiredElement(root, '.landing-tour-block__draft', HTMLElement);
        const scrollIntoView = vi.fn();
        Object.defineProperty(draft, 'scrollIntoView', { value: scrollIntoView });
        expect(itemNames(draft)).toEqual(['Greek yogurt', 'Oat granola', 'Breakfast bowl']);

        click(draft, 'button[aria-label="QUICK_MEAL.COLLAPSE"]');
        harness.detectChanges();
        expect(draft.querySelector('.quick-meal__body')?.getAttribute('aria-hidden')).toBe('true');
        click(draft, 'button[aria-label="QUICK_MEAL.EXPAND"]');
        harness.detectChanges();
        expect(draft.querySelector('.quick-meal__body')?.getAttribute('aria-hidden')).toBe('false');
        click(draft, 'button[aria-label="QUICK_MEAL.CLEAR"]');
        harness.detectChanges();
        expect(draft.querySelector('.quick-meal__empty')?.textContent.trim()).toBe('QUICK_MEAL.PREVIEW_EMPTY');

        const frames: FrameRequestCallback[] = [];
        const browser = TestBed.inject(BrowserWindowService);
        vi.spyOn(browser, 'requestAnimationFrame').mockImplementation(callback => frames.push(callback));
        const mediaQuery = vi.spyOn(browser, 'matchMedia').mockReturnValue(createMotionPreference(false));
        click(root, yogurtAdd);
        mediaQuery.mockReturnValue(createMotionPreference(true));
        click(root, saladAdd);
        harness.detectChanges();

        expect(itemNames(draft)).toEqual(['Greek yogurt', 'Chicken salad']);
        expect(mediaQuery).toHaveBeenCalledWith('(prefers-reduced-motion: reduce)');
        expect(scrollIntoView).not.toHaveBeenCalled();
        for (const frame of frames) {
            frame(0);
        }
        expect(scrollIntoView.mock.calls).toEqual([
            [{ behavior: 'smooth', block: 'center', inline: 'nearest' }],
            [{ behavior: 'instant', block: 'center', inline: 'nearest' }],
        ]);
        click(draft, saveDraft);
        TestBed.inject(HttpTestingController).verify();
        expect(itemNames(draft)).toEqual(['Greek yogurt', 'Chicken salad']);
    });

    it('renders the sample in the language of the new public route', async () => {
        const { harness, root, translate } = await renderLandingAsync('/');
        expect(itemNames(root)).toContain('Greek yogurt');
        const russianYogurt = 'Греческий йогурт';
        translate.setTranslation('ru', {
            LANDING_PREVIEW_TOUR: {
                PREVIEW_DATA: { PRODUCTS: { YOGURT: { NAME: russianYogurt } } },
            },
        });

        await harness.navigateByUrl('/ru', MainComponent);
        const localizedLanding = await revealPreviewAsync(harness);

        expect(localizedLanding.querySelector('fd-product-card button.entity-card__open-button')?.getAttribute('aria-label')).toBe(
            russianYogurt,
        );
        expect(itemNames(localizedLanding)).toContain(russianYogurt);
    });
});

describe('Public landing demo sign-in', () => {
    it.each([
        ['fd-product-card button.entity-card__open-button', 'login'],
        ['fd-recipe-card:last-child button.entity-card__open-button', 'login'],
        ['fd-meals-preview .meals-preview__view-all button', 'login'],
        ['button.meals-preview__placeholder-main', 'register'],
        ['button.nutrition-trend__details', 'register'],
        ['.hero__cta--primary button', 'register'],
        ['.landing-steps__cta-actions fd-ui-button[variant="primary"] button', 'register'],
        ['.landing-steps__cta-actions fd-ui-button[variant="secondary"] button', 'login'],
        ['.dietologist-promo__actions fd-ui-button[variant="primary"] button', 'register'],
        ['.dietologist-promo__actions fd-ui-button[variant="secondary"] button', 'login'],
    ] as const)('keeps the guest draft after %s opens and dismisses %s', async (selector, mode) => {
        const { harness, root, dialog, closed } = await renderLandingAsync('/ru');
        const router = TestBed.inject(Router);
        const draft = requiredElement(root, '.landing-tour-block__draft', HTMLElement);
        click(draft, 'button[aria-label="QUICK_MEAL.CLEAR"]');
        vi.spyOn(TestBed.inject(BrowserWindowService), 'matchMedia').mockReturnValue(null);
        click(root, saladAdd);
        harness.detectChanges();
        expect(itemNames(draft)).toEqual(['Chicken salad']);

        click(root, selector);
        await harness.fixture.whenStable();
        expect(router.url).toBe(`/ru?auth=${mode}`);
        expect(dialog).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ mode }));

        closed.next();
        closed.complete();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(router.url).toBe('/ru');
        expect(root.querySelector('.landing-tour-block__draft')).toBe(draft);
        expect(itemNames(draft)).toEqual(['Chicken salad']);
    });

    it('clears the sample when sign-in succeeds while the landing dialog is open', async () => {
        const { harness, root, authenticated, dialog, translate } = await renderLandingAsync('/?auth=login');
        expect(dialog).toHaveBeenCalledOnce();
        expect(itemNames(root)).toContain('Greek yogurt');

        authenticated.set(true);
        harness.detectChanges();
        expect(itemNames(root)).toEqual([]);

        translate.setTranslation('ru', {});
        await firstValueFrom(translate.use('ru'));
        harness.detectChanges();
        expect(itemNames(root)).toEqual([]);
    });
});

async function renderLandingAsync(url: string): Promise<LandingFixture> {
    const authenticated = signal(false);
    const closed = new Subject<void>();
    const dialog = vi.fn<PublicAuthDialogService['openAsync']>().mockResolvedValue({
        afterClosed: (): Observable<void> => closed.asObservable(),
    });
    await TestBed.configureTestingModule({
        deferBlockBehavior: DeferBlockBehavior.Manual,
        imports: [MainComponent],
        providers: [
            provideRouter([
                { path: '', component: MainComponent, canActivate: [publicSiteLocaleGuard] },
                { path: 'ru', component: MainComponent, canActivate: [publicSiteLocaleGuard] },
            ]),
            provideHttpClient(withInterceptors([publicTranslationAssets])),
            provideHttpClientTesting(),
            provideTranslateTesting(),
            { provide: AuthService, useValue: { isAuthReady: signal(true), isAuthenticated: authenticated } },
            { provide: PublicAuthDialogService, useValue: { openAsync: dialog } },
        ],
    }).compileComponents();
    const translate = TestBed.inject(TranslateService);
    const fixtureTranslation = { LANDING_PREVIEW_TOUR: { PREVIEW_DATA: { PRODUCTS: products, RECIPES: recipes } } };
    translate.setTranslation('en', fixtureTranslation);
    translate.setTranslation('ru', fixtureTranslation);
    await firstValueFrom(translate.use('en'));
    const harness = await RouterTestingHarness.create(url);
    const root = await revealPreviewAsync(harness);
    return { authenticated, closed, dialog, harness, root, translate };
}

async function revealPreviewAsync(harness: RouterTestingHarness): Promise<HTMLElement> {
    const root = harness.routeNativeElement;
    if (!(root instanceof HTMLElement)) {
        throw new Error('Expected the landing route to render');
    }
    expect(root.querySelector('.landing-main__preview-placeholder')).not.toBeNull();
    for (const block of await harness.fixture.getDeferBlocks()) {
        await block.render(DeferBlockState.Complete);
    }
    harness.detectChanges();
    expect(root.querySelector('fd-landing-preview-tour')).not.toBeNull();
    return root;
}

function createMotionPreference(matches: boolean): MediaQueryList {
    const legacyListeners = {
        addListener: vi.fn(),
        removeListener: vi.fn(),
    };
    return {
        ...legacyListeners,
        matches,
        media: '(prefers-reduced-motion: reduce)',
        onchange: null,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
    };
}

function itemNames(root: HTMLElement): string[] {
    return Array.from(root.querySelectorAll('.quick-meal__name'), item => item.textContent.trim());
}

function click(root: HTMLElement, selector: string): void {
    requiredElement(root, selector, HTMLButtonElement).click();
}

function requiredElement<E extends HTMLElement>(root: HTMLElement, selector: string, elementType: new () => E): E {
    const element = root.querySelector(selector);
    if (!(element instanceof elementType)) {
        throw new Error(`Expected ${selector} to render`);
    }
    return element;
}
