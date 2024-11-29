import { ApiProperty } from '@nestjs/swagger';

export class AggregatedStatisticsDto {
    @ApiProperty({
        description: 'Start date for the aggregated statistics period',
        example: '2023-01-01T00:00:00.000Z',
    })
    dateFrom: Date;

    @ApiProperty({
        description: 'End date for the aggregated statistics period',
        example: '2023-01-07T23:59:59.999Z',
    })
    dateTo: Date;

    @ApiProperty({
        description: 'Total calories consumed during the period',
        example: 2000,
    })
    totalCalories: number;

    @ApiProperty({
        description: 'Average proteins consumed per day during the period',
        example: 50,
    })
    averageProteins: number;

    @ApiProperty({
        description: 'Average fats consumed per day during the period',
        example: 70,
    })
    averageFats: number;

    @ApiProperty({
        description: 'Average carbohydrates consumed per day during the period',
        example: 250,
    })
    averageCarbs: number;

    constructor(
        dateFrom: Date,
        dateTo: Date,
        totalCalories: number,
        averageProteins: number,
        averageFats: number,
        averageCarbs: number,
    ) {
        this.dateFrom = dateFrom;
        this.dateTo = dateTo;
        this.totalCalories = totalCalories;
        this.averageProteins = averageProteins;
        this.averageFats = averageFats;
        this.averageCarbs = averageCarbs;
    }
}
