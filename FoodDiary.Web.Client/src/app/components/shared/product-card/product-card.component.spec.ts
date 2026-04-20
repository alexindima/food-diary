import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ProductCardComponent, ProductCardItem } from './product-card.component';
import { TranslateModule } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
// eslint-disable-next-line no-restricted-imports
import { FavoriteProductService } from '../../../features/products/api/favorite-product.service';
import { AuthService } from '../../../services/auth.service';

describe('ProductCardComponent', () => {
    let component: ProductCardComponent;
    let fixture: ComponentFixture<ProductCardComponent>;

    const mockProduct: ProductCardItem = {
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

    beforeEach(async () => {
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

        fixture = TestBed.createComponent(ProductCardComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('product', mockProduct);
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should display product name', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const nameEl = el.querySelector('.entity-card__name');
        expect(nameEl?.textContent?.trim()).toBe('Test Product');
    });

    it('should emit open on card click', () => {
        fixture.detectChanges();

        const openSpy = vi.fn();
        component.open.subscribe(openSpy);

        const el: HTMLElement = fixture.nativeElement;
        const card = el.querySelector<HTMLElement>('.entity-card');
        card?.click();

        expect(openSpy).toHaveBeenCalledOnce();
    });

    it('should emit addToMeal on add button click', () => {
        fixture.detectChanges();

        const addSpy = vi.fn();
        component.addToMeal.subscribe(addSpy);

        component.handleAdd();

        expect(addSpy).toHaveBeenCalledOnce();
    });

    it('should display calories per base', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const caloriesEl = el.querySelector('.entity-card__calories-value');
        expect(caloriesEl?.textContent?.trim()).toBe('290');
    });

    it('should display quality score progress', () => {
        fixture.detectChanges();

        const el: HTMLElement = fixture.nativeElement;
        const labelEl = el.querySelector('.entity-card__quality-label');
        const valueEl = el.querySelector('.entity-card__quality-value');
        const fillEl = el.querySelector<HTMLElement>('.entity-card__quality-fill');

        expect(labelEl?.textContent?.trim()).toBe('PRODUCT_CARD.QUALITY_SCORE');
        expect(valueEl?.textContent?.trim()).toBe('72');
        expect(fillEl?.style.width).toBe('72%');
    });

    it('should show image when imageUrl is provided', () => {
        fixture.componentRef.setInput('imageUrl', 'https://example.com/photo.jpg');
        fixture.detectChanges();

        const el: HTMLElement = fixture.nativeElement;
        const img = el.querySelector('.entity-card__thumb img');
        expect(img).toBeTruthy();
    });

    it('should show icon when imageUrl is not provided', () => {
        fixture.detectChanges();

        const el: HTMLElement = fixture.nativeElement;
        const icon = el.querySelector('.entity-card__thumb fd-ui-icon');
        expect(icon).toBeTruthy();
    });
});
