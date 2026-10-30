import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
// eslint-disable-next-line scope-imports/shared-ui -- shared card spec needs the concrete feature favorite service token
import { FavoriteProductService } from '../../../features/products/api/favorite-product.service';
import { AuthService } from '../../../services/auth.service';
import { MeasurementUnit } from '../../../shared/models/product.data';
import { ProductCardComponent, type ProductCardItem } from './product-card';

const MOCK_PRODUCT: ProductCardItem = {
    id: 'product-1',
    name: 'Test Product',
    comment: 'Best with grilled chicken',
    isOwnedByCurrentUser: true,
    proteinsPerBase: 20,
    fatsPerBase: 10,
    carbsPerBase: 30,
    fiberPerBase: 5,
    alcoholPerBase: 0,
    caloriesPerBase: 290,
    qualityScore: 72,
    qualityGrade: 'green',
};

const GRAM_NUTRITION_BASE = 100;
const DRINK_NUTRITION_BASE = 250;

type ProductCardTestContext = {
    component: ProductCardComponent;
    el: HTMLElement;
    fixture: ComponentFixture<ProductCardComponent>;
};

async function setupProductCardAsync(): Promise<ProductCardTestContext> {
    await TestBed.configureTestingModule({
        imports: [ProductCardComponent],
        providers: [
            { provide: FdUiDialogService, useValue: { open: vi.fn() } },
            provideTranslateTesting(),
            {
                provide: FavoriteProductService,
                useValue: {
                    isFavorite: (): Observable<boolean> => of(false),
                    add: (): Observable<{ id: string }> => of({ id: 'favorite-product-1' }),
                    remove: (): Observable<void> => of(void 0),
                    getAll: (): Observable<[]> => of([]),
                },
            },
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: signal(true),
                },
            },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ProductCardComponent);
    const component = fixture.componentInstance;
    fixture.componentRef.setInput('product', MOCK_PRODUCT);
    fixture.componentRef.setInput('imageUrl', null);
    const el = fixture.nativeElement as HTMLElement;

    return { component, el, fixture };
}

describe('ProductCardComponent', () => {
    it('opens all photos in cover order without also opening product details', async () => {
        const { component, fixture } = await setupProductCardAsync();
        fixture.componentRef.setInput('imageUrl', '/cover.jpg');
        fixture.componentRef.setInput('product', { ...MOCK_PRODUCT, images: [{ imageUrl: '/cover.jpg' }, { imageUrl: '/label.jpg' }] });
        const dialogOpen = vi.spyOn(TestBed.inject(FdUiDialogService), 'open');
        const opened = vi.fn();
        component.open.subscribe(opened);
        fixture.detectChanges();
        component['previewCardImage']();
        expect(dialogOpen).toHaveBeenCalledOnce();
        expect(dialogOpen.mock.calls[0]?.[1]?.data).toEqual(
            expect.objectContaining({
                imageUrl: undefined,
                collageImages: [
                    { url: '/cover.jpg', alt: MOCK_PRODUCT.name },
                    { url: '/label.jpg', alt: MOCK_PRODUCT.name },
                ],
            }),
        );
        expect(opened).not.toHaveBeenCalled();
    });

    it('should create', async () => {
        const { component, fixture } = await setupProductCardAsync();
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });
});

describe('ProductCardComponent content', () => {
    it.each([
        [GRAM_NUTRITION_BASE, MeasurementUnit.G],
        [DRINK_NUTRITION_BASE, MeasurementUnit.ML],
        [1, MeasurementUnit.PCS],
    ])('shows the declared nutrition basis %s %s without rescaling calories', async (baseAmount, baseUnit) => {
        const { el, fixture } = await setupProductCardAsync();
        fixture.componentRef.setInput('catalogLayout', true);
        fixture.componentRef.setInput('product', { ...MOCK_PRODUCT, baseAmount, baseUnit });
        fixture.detectChanges();

        expect(el.querySelector('.entity-card__calories')?.textContent).toContain(`/ ${baseAmount} GENERAL.UNITS.${baseUnit}`);
        expect(el.querySelector('.entity-card__calories-value')?.textContent.trim()).toBe('290');
    });

    it('keeps the brand in the catalog and leaves barcode and comments to product details', async () => {
        const { el, fixture } = await setupProductCardAsync();
        fixture.componentRef.setInput('catalogLayout', true);
        fixture.componentRef.setInput('product', { ...MOCK_PRODUCT, brand: 'Example brand', barcode: '1234567890123' });
        fixture.detectChanges();

        expect(el.querySelector('.entity-card__description')?.textContent.trim()).toBe('Example brand');
        expect(el.textContent).not.toContain('1234567890123');
        expect(el.querySelector('.entity-card__comment')).toBeNull();
    });

    it('keeps catalog nutrition and actions available when quality is absent', async () => {
        const { el, fixture } = await setupProductCardAsync();
        fixture.componentRef.setInput('catalogLayout', true);
        fixture.componentRef.setInput('product', { ...MOCK_PRODUCT, qualityScore: null, qualityGrade: null });
        fixture.detectChanges();

        expect(el.querySelector('.nutrient-badges__chip--quality')).toBeNull();
        expect(el.querySelector('.entity-card__calories-value')?.textContent.trim()).toBe('290');
        expect(el.querySelector('.entity-card__compact-action')).not.toBeNull();
    });

    it('should display product name', async () => {
        const { el, fixture } = await setupProductCardAsync();
        fixture.detectChanges();

        const nameEl = el.querySelector('.entity-card__name');
        expect(nameEl?.textContent.trim()).toBe('Test Product');
    });

    it('should display calories per base', async () => {
        const { el, fixture } = await setupProductCardAsync();
        fixture.detectChanges();

        const caloriesEl = el.querySelector('.entity-card__calories-value');
        expect(caloriesEl?.textContent.trim()).toBe('290');
    });

    it('should display quality score progress', async () => {
        const { el, fixture } = await setupProductCardAsync();
        fixture.detectChanges();

        const labelEl = el.querySelector('.entity-card__quality-label');
        const valueEl = el.querySelector('.entity-card__quality-value');
        const fillEl = el.querySelector<HTMLElement>('.entity-card__quality-fill');

        expect(labelEl?.textContent.trim()).toBe('PRODUCT_CARD.QUALITY_SCORE');
        expect(valueEl?.textContent.trim()).toBe('72');
        expect(fillEl?.style.width).toBe('72%');
    });

    it('should display product comment', async () => {
        const { el, fixture } = await setupProductCardAsync();
        fixture.detectChanges();

        const commentEl = el.querySelector('.entity-card__comment');
        expect(commentEl).not.toBeNull();
        expect(commentEl?.textContent.trim()).toBe('Best with grilled chicken');
    });

    it('should hide ownership badge when ownership display is disabled', async () => {
        const { el, fixture } = await setupProductCardAsync();
        fixture.componentRef.setInput('showOwnership', false);
        fixture.detectChanges();

        const ownershipBadge = el.querySelector('.entity-card__ownership-badge');
        expect(ownershipBadge).toBeNull();
    });
});

describe('ProductCardComponent events', () => {
    it('keeps the catalog add and favorite controls independent from opening product details', async () => {
        const { component, el, fixture } = await setupProductCardAsync();
        fixture.componentRef.setInput('catalogLayout', true);
        fixture.detectChanges();
        const opened = vi.fn();
        const added = vi.fn();
        const favorited = vi.fn();
        component.open.subscribe(opened);
        component.addToMeal.subscribe(added);
        component.favoriteToggle.subscribe(favorited);

        el.querySelector<HTMLButtonElement>('.entity-card__compact-action')?.click();
        el.querySelector<HTMLButtonElement>('.entity-card__favorite-button')?.click();

        expect(added).toHaveBeenCalledOnce();
        expect(favorited).toHaveBeenCalledOnce();
        expect(opened).not.toHaveBeenCalled();
        expect(el.querySelector('.entity-card__compact-action')?.textContent).not.toContain('PRODUCT_CARD.TO_DIARY');
    });

    it.each(['label', 'bar', 'value'])('opens product details when clicking the quality %s', async part => {
        const { component, el, fixture } = await setupProductCardAsync();
        fixture.detectChanges();
        const opened = vi.fn();
        component.open.subscribe(opened);

        el.querySelector<HTMLElement>(`.entity-card__quality-${part}`)?.click();

        expect(opened).toHaveBeenCalledOnce();
        expect(el.querySelector('.nutrient-badges__explanation')).toBeNull();
    });

    it('does not open product details when activating the quality info button', async () => {
        const { component, el, fixture } = await setupProductCardAsync();
        fixture.detectChanges();
        const opened = vi.fn();
        component.open.subscribe(opened);

        el.querySelector<HTMLButtonElement>('.nutrient-badges__quality-help button')?.click();
        fixture.detectChanges();

        expect(opened).not.toHaveBeenCalled();
        expect(el.ownerDocument.querySelector('[role="tooltip"]')?.textContent).toContain('PRODUCT_CARD.QUALITY_EXPLANATION');
    });

    it('should emit open on card click', async () => {
        const { component, el, fixture } = await setupProductCardAsync();
        fixture.detectChanges();

        const openSpy = vi.fn();
        component['open'].subscribe(openSpy);

        const card = el.querySelector<HTMLElement>('.entity-card');
        card?.click();

        expect(openSpy).toHaveBeenCalledOnce();
    });

    it('should emit addToMeal on add button click', async () => {
        const { component, fixture } = await setupProductCardAsync();
        fixture.detectChanges();

        const addSpy = vi.fn();
        component['addToMeal'].subscribe(addSpy);

        component['addToMealFromCard']();

        expect(addSpy).toHaveBeenCalledOnce();
    });
});

describe('ProductCardComponent thumbnail', () => {
    it('should show image when imageUrl is provided', async () => {
        const { el, fixture } = await setupProductCardAsync();
        fixture.componentRef.setInput('imageUrl', 'https://example.com/photo.jpg');
        fixture.detectChanges();

        const img = el.querySelector('.entity-card__thumb img');
        expect(img).toBeTruthy();
    });

    it('should show icon when imageUrl is not provided', async () => {
        const { el, fixture } = await setupProductCardAsync();
        fixture.detectChanges();

        const icon = el.querySelector('.entity-card__thumb fd-ui-icon');
        expect(icon).toBeTruthy();
    });
});
