import { Controller, Get, Query, Req } from '@nestjs/common';
import { StatisticsService } from '../services/statistics.service';
import { ApiResponseDto } from '../../../dto/api-response.dto';
import { AggregatedStatisticsDto } from '../dto/aggregated-statistics.dto';
import { StatisticsQueryParams } from '../interfaces/statistics-query-params.interface';
import { ApiTags, ApiOperation, ApiResponse, ApiQuery } from '@nestjs/swagger';

@ApiTags('Statistics')
@Controller('statistics')
export class StatisticsController {
    constructor(private readonly statisticsService: StatisticsService) {}

    @Get()
    @ApiOperation({ summary: 'Retrieve aggregated statistics' })
    @ApiQuery({
        name: 'dateFrom',
        required: true,
        description: 'Start date for the statistics period',
        example: '2023-01-01T00:00:00.000Z',
    })
    @ApiQuery({
        name: 'dateTo',
        required: true,
        description: 'End date for the statistics period',
        example: '2023-01-01T00:00:00.000Z',
    })
    @ApiQuery({
        name: 'quantizationDays',
        required: false,
        description: 'Number of days to group data for aggregation',
        example: 7,
    })
    @ApiResponse({
        status: 200,
        description: 'Aggregated statistics retrieved successfully',
        type: [AggregatedStatisticsDto],
    })
    @ApiResponse({
        status: 400,
        description: 'Invalid date format or missing parameters',
    })
    async getAggregatedStatistics(
        @Query() query: StatisticsQueryParams,
        @Req() req: Request,
    ): Promise<ApiResponseDto<AggregatedStatisticsDto[]>> {
        const { dateFrom, dateTo } = query;
        const userId = req['userId'];

        const normalizedDateFrom = new Date(dateFrom);
        normalizedDateFrom.setHours(0, 0, 0, 0);

        const normalizedDateTo = new Date(dateTo);
        normalizedDateTo.setHours(23, 59, 59, 999);

        const statistics = await this.statisticsService.getAggregatedStatistics(
            normalizedDateFrom,
            normalizedDateTo,
            userId,
        );

        return ApiResponseDto.success(statistics);
    }
}
