import type { FavoriteProductHttpResponse } from '../../../shared/api/sdk/generated/model/favorite-product-http-response';
import type { ProductHttpResponse } from '../../../shared/api/sdk/generated/model/product-http-response';
import type { ProductHttpResponsePagedHttpResponse } from '../../../shared/api/sdk/generated/model/product-http-response-paged-http-response';
import type { ProductOverviewHttpResponse } from '../../../shared/api/sdk/generated/model/product-overview-http-response';
import type { PublicProductHttpResponse } from '../../../shared/api/sdk/generated/model/public-product-http-response';
import { requireSdkFields, sdkEnum, sdkInstant, sdkMaybe } from '../../../shared/api/sdk/sdk-response';
import type { PageOf } from '../../../shared/models/page-of.data';
import {
    type FavoriteProduct,
    MeasurementUnit,
    type Product,
    type ProductOverview,
    ProductType,
    ProductVisibility,
} from '../../../shared/models/product.data';
import type { PublicProduct } from '../models/public-product.data';
import { normalizeProductUnit } from './product-unit.mapper';

const QUALITY_GRADES = ['green', 'yellow', 'red'] as const;

export function productFromSdk(response: ProductHttpResponse): Product {
    const value = requireSdkFields(response, [
        'id',
        'name',
        'baseUnit',
        'baseAmount',
        'defaultPortionAmount',
        'caloriesPerBase',
        'proteinsPerBase',
        'fatsPerBase',
        'carbsPerBase',
        'fiberPerBase',
        'alcoholPerBase',
        'usageCount',
        'visibility',
        'createdAt',
        'isOwnedByCurrentUser',
        'qualityScore',
        'qualityGrade',
    ]);
    return {
        ...value,
        baseUnit: sdkEnum(value.baseUnit.toUpperCase(), Object.values(MeasurementUnit)),
        visibility: sdkEnum(value.visibility, Object.values(ProductVisibility)),
        productType: sdkMaybe(value.productType, type => sdkEnum(type, Object.values(ProductType), ProductType.Unknown)),
        qualityGrade: sdkEnum(value.qualityGrade, QUALITY_GRADES),
        createdAt: sdkInstant(value.createdAt),
        images: value.images?.map(image => ({
            imageAssetId: image.imageAssetId ?? null,
            imageUrl: requireSdkFields(image, ['imageUrl']).imageUrl,
        })),
    };
}

export function favoriteProductFromSdk(response: FavoriteProductHttpResponse): FavoriteProduct {
    const value = requireSdkFields(response, [
        'id',
        'productId',
        'createdAtUtc',
        'productName',
        'baseUnit',
        'preferredPortionAmount',
        'defaultPortionAmount',
        'caloriesPerBase',
        'proteinsPerBase',
        'fatsPerBase',
        'carbsPerBase',
        'fiberPerBase',
        'alcoholPerBase',
        'qualityScore',
        'qualityGrade',
        'isOwnedByCurrentUser',
    ]);
    return {
        ...normalizeProductUnit(value),
        imageUrls: value.imageUrls ?? undefined,
        qualityGrade: sdkEnum(value.qualityGrade, QUALITY_GRADES),
    };
}

export function productPageFromSdk(response: ProductHttpResponsePagedHttpResponse): PageOf<Product> {
    const value = requireSdkFields(response, ['data', 'page', 'limit', 'totalPages', 'totalItems']);
    return { ...value, data: value.data.map(productFromSdk) };
}

export function productOverviewFromSdk(response: ProductOverviewHttpResponse): ProductOverview {
    const value = requireSdkFields(response, ['allProducts', 'recentItems', 'favoriteItems', 'favoriteTotalCount']);
    return {
        ...value,
        allProducts: productPageFromSdk(value.allProducts),
        recentItems: value.recentItems.map(productFromSdk),
        favoriteItems: value.favoriteItems.map(favoriteProductFromSdk),
    };
}

export function publicProductFromSdk(response: PublicProductHttpResponse): PublicProduct {
    const value = requireSdkFields(response, [
        'id',
        'name',
        'baseUnit',
        'baseAmount',
        'calories',
        'proteins',
        'fats',
        'carbs',
        'fiber',
        'alcohol',
    ]);
    return { ...value, brand: value.brand ?? null, imageUrl: value.imageUrl ?? null, images: value.images ?? undefined };
}
