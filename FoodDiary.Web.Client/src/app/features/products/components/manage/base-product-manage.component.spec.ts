import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../../services/navigation.service';
import { UsdaService } from '../../../usda/api/usda.service';
import { OpenFoodFactsService } from '../../api/open-food-facts.service';
import { ProductService } from '../../api/product.service';
import { ProductManageFacade } from '../../lib/product-manage.facade';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../models/product.data';
import { BaseProductManageComponent } from './base-product-manage.component';

const PRODUCT: Product = {
    id: 'product-1',
    name: 'Test product',
    barcode: null,
    brand: null,
    productType: ProductType.Unknown,
    category: null,
    description: null,
    comment: null,
    imageUrl: null,
    imageAssetId: null,
    baseUnit: MeasurementUnit.G,
    baseAmount: 100,
    defaultPortionAmount: 100,
    caloriesPerBase: 100,
    proteinsPerBase: 10,
    fatsPerBase: 5,
    carbsPerBase: 12,
    fiberPerBase: 1,
    alcoholPerBase: 0,
    usageCount: 0,
    visibility: ProductVisibility.Private,
    createdAt: new Date('2026-01-01T00:00:00Z'),
    isOwnedByCurrentUser: true,
    qualityScore: 80,
    qualityGrade: 'green',
};

describe('BaseProductManageComponent header state', () => {
    let fixture: ComponentFixture<BaseProductManageComponent>;
    let component: BaseProductManageComponent;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [BaseProductManageComponent, TranslateModule.forRoot()],
            providers: [
                {
                    provide: ProductService,
                    useValue: {
                        searchSuggestions: vi.fn().mockReturnValue(of([])),
                    },
                },
                {
                    provide: OpenFoodFactsService,
                    useValue: {
                        searchByBarcode: vi.fn().mockReturnValue(of(null)),
                    },
                },
                {
                    provide: UsdaService,
                    useValue: {
                        getFoodDetail: vi.fn().mockReturnValue(of(null)),
                    },
                },
                {
                    provide: FdUiDialogService,
                    useValue: {
                        open: vi.fn().mockReturnValue({ afterClosed: () => of(null) }),
                    },
                },
                {
                    provide: NavigationService,
                    useValue: {
                        navigateToProductListAsync: vi.fn().mockResolvedValue(undefined),
                    },
                },
                {
                    provide: ProductManageFacade,
                    useValue: {
                        ensurePremiumAccess: vi.fn().mockReturnValue(true),
                        confirmDiscardChangesAsync: vi.fn().mockResolvedValue(true),
                        deleteProductAsync: vi.fn().mockResolvedValue('deleted'),
                        submitProductAsync: vi.fn().mockResolvedValue({ product: null, error: null }),
                    },
                },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(BaseProductManageComponent);
        component = fixture.componentInstance;
    });

    it('should use create title and submit label when product is not provided', () => {
        expect(component.manageHeaderState()).toEqual({
            titleKey: 'PRODUCT_MANAGE.ADD_TITLE',
            submitIcon: 'add',
            submitLabelKey: 'PRODUCT_MANAGE.ADD_BUTTON',
        });
    });

    it('should use edit title and submit label when product is provided', () => {
        fixture.componentRef.setInput('product', PRODUCT);

        expect(component.manageHeaderState()).toEqual({
            titleKey: 'PRODUCT_MANAGE.EDIT_TITLE',
            submitIcon: 'save',
            submitLabelKey: 'PRODUCT_MANAGE.SAVE_BUTTON',
        });
    });
});
