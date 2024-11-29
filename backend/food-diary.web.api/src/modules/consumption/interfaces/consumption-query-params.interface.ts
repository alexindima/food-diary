import { BaseQueryParams } from '../../../interfaces/base-query-params.interface';

export interface ConsumptionQueryParams extends BaseQueryParams {
    dateFrom?: string;
    dateTo?: string;
}
