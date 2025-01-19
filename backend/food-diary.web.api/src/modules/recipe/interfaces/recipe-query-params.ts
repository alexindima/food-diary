import { BaseQueryParams } from '../../../interfaces/base-query-params.interface';

export interface RecipeQueryParams extends BaseQueryParams {
    name?: string;
    category?: string;
    difficulty?: string;
    prepTimeFrom?: number;
    prepTimeTo?: number;
    cookTimeFrom?: number;
    cookTimeTo?: number;
}
