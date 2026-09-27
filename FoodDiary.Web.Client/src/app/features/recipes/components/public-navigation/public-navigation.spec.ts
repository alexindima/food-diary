import { DOCUMENT } from '@angular/common';
import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService, FdUiMenuItemComponent } from 'fd-ui-kit';
import { beforeEach, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { ThemeService } from '../../../../shared/theme/theme.service';
import { PublicAuthDialogService } from '../../../public/lib/public-auth-dialog.service';
import { PublicRecipeNavigationComponent } from './public-navigation';

const authenticated = signal(false);
const appearanceDialog = { open: vi.fn() };
const dialog = { openAsync: vi.fn().mockResolvedValue(undefined) };
const localization = {
    applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(undefined),
    loadTranslationsForRouteAsync: vi.fn().mockResolvedValue(undefined),
};
beforeEach(() => {
    authenticated.set(false);
    vi.clearAllMocks();
    TestBed.configureTestingModule({
        imports: [PublicRecipeNavigationComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: AuthService, useValue: { isAuthenticated: authenticated } },
            { provide: PublicAuthDialogService, useValue: dialog },
            { provide: FdUiDialogService, useValue: appearanceDialog },
            { provide: LocalizationService, useValue: localization },
        ],
    });
    TestBed.inject(TranslateService).use('en');
});
function create(): ComponentFixture<PublicRecipeNavigationComponent> {
    const fixture = TestBed.createComponent(PublicRecipeNavigationComponent);
    fixture.detectChanges();
    return fixture;
}
function click(root: ParentNode, selector: string): void {
    const button = root.querySelector<HTMLButtonElement>(selector);
    expect(button).not.toBeNull();
    button?.click();
}
it('links the brand home and keeps guest controls out of the authenticated shell', () => {
    const fixture = create();
    expect((fixture.nativeElement as HTMLElement).querySelector('.brand')?.getAttribute('href')).toBe('/');
    expect((fixture.nativeElement as HTMLElement).querySelector('.brand-mark')).not.toBeNull();
    authenticated.set(true);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('nav')).toBeNull();
});
it('changes language through shared localization and loads the current route translations', async () => {
    const fixture = create();
    click(fixture.nativeElement as HTMLElement, '[aria-label="PUBLIC_RECIPES.INTERFACE_LANGUAGE"]');
    fixture.detectChanges();
    click(TestBed.inject(DOCUMENT), 'fd-ui-menu-item button');
    await fixture.whenStable();
    expect(localization.applyLanguagePreferenceAsync).toHaveBeenCalledWith('ru');
    expect(localization.loadTranslationsForRouteAsync).toHaveBeenCalledWith(TestBed.inject(Router).url);
});
it('opens the shared appearance dialog with local guest persistence', async () => {
    const theme = TestBed.inject(ThemeService);
    const fixture = create();
    click(fixture.nativeElement as HTMLElement, '[aria-label="PUBLIC_RECIPES.THEME"]');
    await vi.waitFor(() => {
        expect(appearanceDialog.open).toHaveBeenCalled();
    });
    expect(appearanceDialog.open).toHaveBeenCalledWith(expect.any(Function), {
        size: 'md',
        data: { theme: theme.theme(), uiStyle: theme.uiStyle(), persistence: 'local' },
    });
});
it('keeps catalog, about and sign in available in the mobile menu', () => {
    const fixture = create();
    click(fixture.nativeElement as HTMLElement, '.mobile-menu button');
    fixture.detectChanges();
    const document = TestBed.inject(DOCUMENT);
    const links = fixture.debugElement
        .queryAll(By.directive(FdUiMenuItemComponent))
        .map(element => element.injector.get(FdUiMenuItemComponent).routerLink());
    expect(links).toContain('/explore');
    expect(links).toContain('/food-diary');
    const items = Array.from(document.querySelectorAll<HTMLButtonElement>('fd-ui-menu-item button'));
    items.at(-1)?.click();
    expect(dialog.openAsync).toHaveBeenCalledWith(expect.objectContaining({ mode: 'login', returnUrl: TestBed.inject(Router).url }));
});
