import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { type Observable, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

// eslint-disable-next-line no-restricted-imports -- shared card spec needs the concrete feature favorite service token
import { FavoriteProductService } from '../../../features/products/api/favorite-product.service';
import { AuthService } from '../../../services/auth.service';
import { ProductCardComponent, type ProductCardItem } from './product-card.component';

const MOCK_PRODUCT: ProductCardItem = {
    id: 'product-1',
    name: 'Test Product',
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

type ProductCardTestContext = {
    component: ProductCardComponent;
    el: HTMLElement;
    fixture: ComponentFixture<ProductCardComponent>;
};

async function setupProductCardAsync(): Promise<ProductCardTestContext> {
    await TestBed.configureTestingModule({
        imports: [ProductCardComponent, TranslateModule.forRoot()],
        providers: [
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
    it('should create', async () => {
        const { component, fixture } = await setupProductCardAsync();
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });
});

describe('ProductCardComponent content', () => {
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
});

describe('ProductCardComponent events', () => {
    it('should emit open on card click', async () => {
        const { component, el, fixture } = await setupProductCardAsync();
        fixture.detectChanges();

        const openSpy = vi.fn();
        component.open.subscribe(openSpy);

        const card = el.querySelector<HTMLElement>('.entity-card');
        card?.click();

        expect(openSpy).toHaveBeenCalledOnce();
    });

    it('should emit addToMeal on add button click', async () => {
        const { component, fixture } = await setupProductCardAsync();
        fixture.detectChanges();

        const addSpy = vi.fn();
        component.addToMeal.subscribe(addSpy);

        component.handleAdd();

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
