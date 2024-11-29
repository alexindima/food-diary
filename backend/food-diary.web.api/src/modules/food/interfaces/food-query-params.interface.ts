import { BaseQueryParams } from '../../../interfaces/base-query-params.interface';

export interface FoodQueryParams extends BaseQueryParams {
    search?: string;
}
