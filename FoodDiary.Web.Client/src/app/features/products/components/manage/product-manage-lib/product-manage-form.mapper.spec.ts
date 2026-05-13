import { describe, expect, it } from 'vitest';

import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import type { ProductAiRecognitionResult } from '../../../dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog.types';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../models/product.data';
import {
    buildAiResultPatch,
    buildConvertedNutritionPatch,
    buildProductData,
    buildProductFormPatch,
    createProductForm,
    getDefaultProductBaseAmount,
    normalizeProductNutritionValues,
} from './product-manage-form.mapper';

const DEFAULT_BASE_AMOUNT = 100;
const PRODUCT: Product = {
    id: 'product-1',
    name: 'Test product',
    barcode: '4600000000000',
    brand: 'Brand',
    productType: ProductType.Dairy,
    category: ProductType.Dairy,
    description: 'Description',
    comment: 'Comment',
    imageUrl: 'https://example.test/product.jpg',
    imageAssetId: 'asset-1',
    baseUnit: MeasurementUnit.G,
    baseAmount: 50,
    defaultPortionAmount: 125,
    caloriesPerBase: 120,
    proteinsPerBase: 7.25,
    fatsPerBase: 3.25,
    carbsPerBase: 11.25,
    fiberPerBase: 1.25,
    alcoholPerBase: 0.25,
    usageCount: 0,
    visibility: ProductVisibility.Public,
    createdAt: new Date('2026-01-01T00:00:00Z'),
    isOwnedByCurrentUser: true,
    qualityScore: 80,
    qualityGrade: 'green',
    usdaFdcId: 123,
};

describe('product manage form creation', () => {
    it('should create form with default base values', () => {
        const form = createProductForm();

        expect(form.controls.name.value).toBe('');
        expect(form.controls.baseAmount.value).toBe(DEFAULT_BASE_AMOUNT);
        expect(form.controls.defaultPortionAmount.value).toBe(DEFAULT_BASE_AMOUNT);
        expect(form.controls.baseUnit.value).toBe(MeasurementUnit.G);
        expect(form.controls.productType.value).toBe(ProductType.Unknown);
        expect(form.controls.visibility.value).toBe(ProductVisibility.Private);
    });

    it('should resolve default base amount by unit', () => {
        expect(getDefaultProductBaseAmount(MeasurementUnit.PCS)).toBe(1);
        expect(getDefaultProductBaseAmount(MeasurementUnit.G)).toBe(DEFAULT_BASE_AMOUNT);
        expect(getDefaultProductBaseAmount(MeasurementUnit.ML)).toBe(DEFAULT_BASE_AMOUNT);
    });
});

describe('product manage request mapping', () => {
    it('should build product request and normalize portion nutrition back to base', () => {
        const image: ImageSelection = {
            url: 'https://example.test/image.png',
            assetId: 'asset-42',
        };
        const form = createProductForm();
        form.patchValue({
            name: 'Portion product',
            barcode: '123',
            brand: 'Brand',
            productType: ProductType.Beverage,
            description: 'Description',
            comment: 'Comment',
            imageUrl: image,
            baseUnit: MeasurementUnit.G,
            defaultPortionAmount: 50,
            caloriesPerBase: 150,
            proteinsPerBase: 5,
            fatsPerBase: 2,
            carbsPerBase: 20,
            fiberPerBase: 1,
            alcoholPerBase: 0.5,
            visibility: ProductVisibility.Public,
        });

        expect(buildProductData(form, 'portion')).toEqual({
            name: 'Portion product',
            barcode: '123',
            brand: 'Brand',
            productType: ProductType.Beverage,
            category: ProductType.Beverage,
            description: 'Description',
            comment: 'Comment',
            imageUrl: image.url,
            imageAssetId: image.assetId,
            baseAmount: DEFAULT_BASE_AMOUNT,
            defaultPortionAmount: 50,
            baseUnit: MeasurementUnit.G,
            caloriesPerBase: 300,
            proteinsPerBase: 10,
            fatsPerBase: 4,
            carbsPerBase: 40,
            fiberPerBase: 2,
            alcoholPerBase: 1,
            visibility: ProductVisibility.Public,
        });
    });

    it('should build edit patch and normalize legacy product nutrition to the current base amount', () => {
        expect(buildProductFormPatch(PRODUCT)).toEqual({
            name: PRODUCT.name,
            barcode: PRODUCT.barcode,
            brand: PRODUCT.brand,
            productType: ProductType.Dairy,
            description: PRODUCT.description,
            comment: PRODUCT.comment,
            imageUrl: {
                url: PRODUCT.imageUrl,
                assetId: PRODUCT.imageAssetId,
            },
            baseAmount: DEFAULT_BASE_AMOUNT,
            defaultPortionAmount: PRODUCT.defaultPortionAmount,
            baseUnit: MeasurementUnit.G,
            caloriesPerBase: 240,
            proteinsPerBase: 14.5,
            fatsPerBase: 6.5,
            carbsPerBase: 22.5,
            fiberPerBase: 2.5,
            alcoholPerBase: 0.5,
            visibility: ProductVisibility.Public,
            usdaFdcId: PRODUCT.usdaFdcId,
        });
    });
});

describe('product manage nutrition mapping', () => {
    it('should convert only filled nutrition controls when nutrition mode changes', () => {
        const form = createProductForm();
        form.patchValue({
            caloriesPerBase: 111.11,
            proteinsPerBase: null,
            fatsPerBase: 3.33,
            carbsPerBase: 4.44,
            fiberPerBase: null,
            alcoholPerBase: 0.55,
        });

        expect(buildConvertedNutritionPatch(form, 2)).toEqual({
            caloriesPerBase: 222.2,
            fatsPerBase: 6.7,
            carbsPerBase: 8.9,
            alcoholPerBase: 1.1,
        });
    });

    it('should build AI result patch while preserving existing optional values when AI leaves them empty', () => {
        const form = createProductForm();
        const image: ImageSelection = { url: 'https://example.test/current.png', assetId: 'current-asset' };
        form.patchValue({
            name: 'Existing name',
            description: 'Existing description',
            imageUrl: image,
        });
        const aiResult: ProductAiRecognitionResult = {
            name: '',
            description: null,
            image: null,
            baseAmount: 30,
            baseUnit: MeasurementUnit.PCS,
            caloriesPerBase: 123.44,
            proteinsPerBase: 9.96,
            fatsPerBase: 2.04,
            carbsPerBase: 11.05,
            fiberPerBase: 1.01,
            alcoholPerBase: 0,
        };

        expect(buildAiResultPatch(form, aiResult)).toEqual({
            name: 'Existing name',
            description: 'Existing description',
            imageUrl: image,
            baseAmount: 1,
            baseUnit: MeasurementUnit.PCS,
            caloriesPerBase: 123.4,
            proteinsPerBase: 10,
            fatsPerBase: 2,
            carbsPerBase: 11.1,
            fiberPerBase: 1,
            alcoholPerBase: 0,
            defaultPortionAmount: 30,
        });
    });

    it('should normalize nutrition values defensively when source amount is missing or invalid', () => {
        const values = {
            caloriesPerBase: 100.04,
            proteinsPerBase: 10.04,
            fatsPerBase: null,
            carbsPerBase: 20.05,
            fiberPerBase: null,
            alcoholPerBase: 0,
        };

        expect(normalizeProductNutritionValues(values, 0, DEFAULT_BASE_AMOUNT)).toEqual({
            caloriesPerBase: DEFAULT_BASE_AMOUNT,
            proteinsPerBase: 10,
            fatsPerBase: null,
            carbsPerBase: 20.1,
            fiberPerBase: null,
            alcoholPerBase: 0,
        });
    });
});
