import { ComponentFixture } from '@angular/core/testing';
import { of } from 'rxjs';

/**
 * Creates a mock MatDialogRef with common spy methods.
 */
export function createDialogRefSpy(): { close: ReturnType<typeof vi.fn> } {
    return { close: vi.fn() };
}

/**
 * Creates a mock TranslateService with instant() and stream().
 */
export function createTranslateServiceSpy(): {
    instant: ReturnType<typeof vi.fn>;
    stream: ReturnType<typeof vi.fn>;
    get: ReturnType<typeof vi.fn>;
    currentLang: string;
    onLangChange: { subscribe: ReturnType<typeof vi.fn> };
} {
    return {
        instant: vi.fn((key: string) => key),
        stream: vi.fn((key: string) => of(key)),
        get: vi.fn((key: string) => of(key)),
        currentLang: 'en',
        onLangChange: { subscribe: vi.fn() },
    };
}

/**
 * Creates a mock NavigationService.
 */
export function createNavigationServiceSpy(): {
    navigateTo: ReturnType<typeof vi.fn>;
    navigateBack: ReturnType<typeof vi.fn>;
    navigateToLogin: ReturnType<typeof vi.fn>;
} {
    return {
        navigateTo: vi.fn().mockReturnValue(Promise.resolve(true)),
        navigateBack: vi.fn().mockReturnValue(Promise.resolve(true)),
        navigateToLogin: vi.fn().mockReturnValue(Promise.resolve(true)),
    };
}

/**
 * Creates a mock FdUiDialogService.
 */
export function createDialogServiceSpy(): {
    open: ReturnType<typeof vi.fn>;
} {
    const ref = createDialogRefSpy();
    return {
        open: vi.fn().mockReturnValue({ ...ref, afterClosed: () => of(undefined) }),
    };
}

/**
 * Creates a mock FdUiToastService.
 */
export function createToastServiceSpy(): {
    open: ReturnType<typeof vi.fn>;
    success: ReturnType<typeof vi.fn>;
    error: ReturnType<typeof vi.fn>;
    info: ReturnType<typeof vi.fn>;
} {
    return {
        open: vi.fn(),
        success: vi.fn(),
        error: vi.fn(),
        info: vi.fn(),
    };
}

/**
 * Creates a mock QuickMealService.
 */
export function createQuickMealServiceSpy(): {
    addProduct: ReturnType<typeof vi.fn>;
    addRecipe: ReturnType<typeof vi.fn>;
    items: ReturnType<typeof vi.fn>;
    hasItems: ReturnType<typeof vi.fn>;
} {
    return {
        addProduct: vi.fn(),
        addRecipe: vi.fn(),
        items: vi.fn().mockReturnValue([]),
        hasItems: vi.fn().mockReturnValue(false),
    };
}

/**
 * Creates a mock BreakpointObserver.
 */
export function createBreakpointObserverSpy(matches = false): {
    observe: ReturnType<typeof vi.fn>;
} {
    return {
        observe: vi.fn().mockReturnValue(of({ matches, breakpoints: {} })),
    };
}

/**
 * Helper to set multiple signal inputs on a component fixture.
 */
export function setInputs<T>(fixture: ComponentFixture<T>, inputs: Record<string, unknown>): void {
    for (const [key, value] of Object.entries(inputs)) {
        fixture.componentRef.setInput(key, value);
    }
    fixture.detectChanges();
}

/**
 * Creates a mock service that returns observables.
 * Pass method names and their default return values.
 */
export function createServiceSpy<T extends Record<string, unknown>>(methods: T): { [K in keyof T]: ReturnType<typeof vi.fn> } {
    const spy = Object.fromEntries(Object.entries(methods).map(([key, value]) => [key, vi.fn().mockReturnValue(value)]));
    return spy as { [K in keyof T]: ReturnType<typeof vi.fn> };
}
