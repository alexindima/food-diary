import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';

import { ProductDetailComponent } from './product-detail.component';
import { Product, MeasurementUnit, ProductVisibility } from '../../models/product.data';
import { ProductService } from '../../api/product.service';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

const mockProduct: Product = {
    id: '1',
    name: 'Test Product',
    isOwnedByCurrentUser: true,
    baseUnit: MeasurementUnit.G,
    baseAmount: 100,
    defaultPortionAmount: 100,
    caloriesPerBase: 165,
    proteinsPerBase: 31,
    fatsPerBase: 3.6,
    carbsPerBase: 0,
    fiberPerBase: 0,
    alcoholPerBase: 0,
    visibility: ProductVisibility.Private,
    usageCount: 0,
    createdAt: new Date('2024-01-01'),
    qualityScore: 80,
    qualityGrade: 'green',
};

describe('ProductDetailComponent', () => {
    let component: ProductDetailComponent;
    let fixture: ComponentFixture<ProductDetailComponent>;

    const mockDialogRef = {
        close: vi.fn(),
    };

    const mockConfirmDialogRef = {
        afterClosed: vi.fn().mockReturnValue(of(true)),
    };

    const mockFdDialogService = {
        open: vi.fn().mockReturnValue(mockConfirmDialogRef),
    };

    const mockProductService = {
        duplicate: vi.fn().mockReturnValue(of({ ...mockProduct, id: '2', name: 'Test Product (Copy)' })),
    };

    beforeEach(async () => {
        vi.clearAllMocks();

        await TestBed.configureTestingModule({
            imports: [ProductDetailComponent, TranslateModule.forRoot()],
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                provideNoopAnimations(),
                { provide: FD_UI_DIALOG_DATA, useValue: mockProduct },
                { provide: FdUiDialogRef, useValue: mockDialogRef },
                { provide: FdUiDialogService, useValue: mockFdDialogService },
                { provide: ProductService, useValue: mockProductService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(ProductDetailComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should display product nutrition', () => {
        expect(component.calories).toBe(165);
        expect(component.nutrientChartData.proteins).toBe(31);
        expect(component.nutrientChartData.fats).toBe(3.6);
        expect(component.nutrientChartData.carbs).toBe(0);
    });

    it('should have summary and nutrients tabs', () => {
        expect(component.tabs.length).toBe(2);
        expect(component.tabs[0].value).toBe('summary');
        expect(component.tabs[1].value).toBe('nutrients');
    });

    it('should change active tab', () => {
        expect(component.activeTab).toBe('summary');

        component.onTabChange('nutrients');
        expect(component.activeTab).toBe('nutrients');

        component.onTabChange('summary');
        expect(component.activeTab).toBe('summary');
    });

    it('should emit edit action', () => {
        component.onEdit();

        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '1', action: 'Edit' }));
    });

    it('should emit delete action after confirmation', () => {
        component.onDelete();

        expect(mockFdDialogService.open).toHaveBeenCalled();
        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '1', action: 'Delete' }));
    });

    it('should detect if user can modify (owned, no usage)', () => {
        expect(component.canModify).toBe(true);
        expect(component.isEditDisabled).toBe(false);
        expect(component.isDeleteDisabled).toBe(false);
    });

    it('should disable edit and delete when not owned by current user', async () => {
        const notOwnedProduct: Product = { ...mockProduct, isOwnedByCurrentUser: false };

        await TestBed.resetTestingModule()
            .configureTestingModule({
                imports: [ProductDetailComponent, TranslateModule.forRoot()],
                providers: [
                    provideHttpClient(),
                    provideHttpClientTesting(),
                    provideNoopAnimations(),
                    { provide: FD_UI_DIALOG_DATA, useValue: notOwnedProduct },
                    { provide: FdUiDialogRef, useValue: mockDialogRef },
                    { provide: FdUiDialogService, useValue: mockFdDialogService },
                    { provide: ProductService, useValue: mockProductService },
                ],
            })
            .compileComponents();

        const notOwnedFixture = TestBed.createComponent(ProductDetailComponent);
        const notOwnedComponent = notOwnedFixture.componentInstance;
        notOwnedFixture.detectChanges();

        expect(notOwnedComponent.canModify).toBe(false);
        expect(notOwnedComponent.isEditDisabled).toBe(true);
        expect(notOwnedComponent.isDeleteDisabled).toBe(true);
    });

    it('should disable edit and delete when product has usage', async () => {
        const usedProduct: Product = { ...mockProduct, usageCount: 5 };

        await TestBed.resetTestingModule()
            .configureTestingModule({
                imports: [ProductDetailComponent, TranslateModule.forRoot()],
                providers: [
                    provideHttpClient(),
                    provideHttpClientTesting(),
                    provideNoopAnimations(),
                    { provide: FD_UI_DIALOG_DATA, useValue: usedProduct },
                    { provide: FdUiDialogRef, useValue: mockDialogRef },
                    { provide: FdUiDialogService, useValue: mockFdDialogService },
                    { provide: ProductService, useValue: mockProductService },
                ],
            })
            .compileComponents();

        const usedFixture = TestBed.createComponent(ProductDetailComponent);
        const usedComponent = usedFixture.componentInstance;
        usedFixture.detectChanges();

        expect(usedComponent.canModify).toBe(false);
    });

    it('should not emit edit when edit is disabled', async () => {
        const usedProduct: Product = { ...mockProduct, usageCount: 5 };

        await TestBed.resetTestingModule()
            .configureTestingModule({
                imports: [ProductDetailComponent, TranslateModule.forRoot()],
                providers: [
                    provideHttpClient(),
                    provideHttpClientTesting(),
                    provideNoopAnimations(),
                    { provide: FD_UI_DIALOG_DATA, useValue: usedProduct },
                    { provide: FdUiDialogRef, useValue: mockDialogRef },
                    { provide: FdUiDialogService, useValue: mockFdDialogService },
                    { provide: ProductService, useValue: mockProductService },
                ],
            })
            .compileComponents();

        vi.clearAllMocks();
        const usedFixture = TestBed.createComponent(ProductDetailComponent);
        const usedComponent = usedFixture.componentInstance;
        usedFixture.detectChanges();

        usedComponent.onEdit();
        expect(mockDialogRef.close).not.toHaveBeenCalled();
    });

    it('should not emit delete when delete is disabled', async () => {
        const usedProduct: Product = { ...mockProduct, usageCount: 5 };

        await TestBed.resetTestingModule()
            .configureTestingModule({
                imports: [ProductDetailComponent, TranslateModule.forRoot()],
                providers: [
                    provideHttpClient(),
                    provideHttpClientTesting(),
                    provideNoopAnimations(),
                    { provide: FD_UI_DIALOG_DATA, useValue: usedProduct },
                    { provide: FdUiDialogRef, useValue: mockDialogRef },
                    { provide: FdUiDialogService, useValue: mockFdDialogService },
                    { provide: ProductService, useValue: mockProductService },
                ],
            })
            .compileComponents();

        vi.clearAllMocks();
        const usedFixture = TestBed.createComponent(ProductDetailComponent);
        const usedComponent = usedFixture.componentInstance;
        usedFixture.detectChanges();

        usedComponent.onDelete();
        expect(mockFdDialogService.open).not.toHaveBeenCalled();
    });

    it('should handle duplicate', () => {
        component.onDuplicate();

        expect(mockProductService.duplicate).toHaveBeenCalledWith('1');
        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '2', action: 'Duplicate' }));
    });

    it('should build macro blocks with correct values', () => {
        expect(component.macroBlocks.length).toBe(5);
        expect(component.macroBlocks[0].value).toBe(31); // proteins
        expect(component.macroBlocks[1].value).toBe(3.6); // fats
        expect(component.macroBlocks[2].value).toBe(0); // carbs
    });

    it('should show warning message when product is not owned', async () => {
        const notOwnedProduct: Product = { ...mockProduct, isOwnedByCurrentUser: false };

        await TestBed.resetTestingModule()
            .configureTestingModule({
                imports: [ProductDetailComponent, TranslateModule.forRoot()],
                providers: [
                    provideHttpClient(),
                    provideHttpClientTesting(),
                    provideNoopAnimations(),
                    { provide: FD_UI_DIALOG_DATA, useValue: notOwnedProduct },
                    { provide: FdUiDialogRef, useValue: mockDialogRef },
                    { provide: FdUiDialogService, useValue: mockFdDialogService },
                    { provide: ProductService, useValue: mockProductService },
                ],
            })
            .compileComponents();

        const notOwnedFixture = TestBed.createComponent(ProductDetailComponent);
        const notOwnedComponent = notOwnedFixture.componentInstance;
        notOwnedFixture.detectChanges();

        expect(notOwnedComponent.warningMessage).toBe('PRODUCT_DETAIL.WARNING_NOT_OWNER');
    });

    it('should have no warning message when product can be modified', () => {
        expect(component.warningMessage).toBeNull();
    });
});
