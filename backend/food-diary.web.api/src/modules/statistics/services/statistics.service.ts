import { Injectable } from '@nestjs/common';
import { PrismaService } from '../../../../prisma/prisma.service';
import { AggregatedStatisticsDto } from '../dto/aggregated-statistics.dto';

@Injectable()
export class StatisticsService {
    constructor(private readonly prisma: PrismaService) {}

    public async getAggregatedStatistics(
        dateFrom: Date,
        dateTo: Date,
        userId: number,
    ): Promise<AggregatedStatisticsDto[]> {
        const results: AggregatedStatisticsDto[] = [];

        let startOfPeriod = new Date(dateFrom);

        while (startOfPeriod.getTime() <= dateTo.getTime()) {
            const currentPeriodStart = new Date(startOfPeriod);
            const currentPeriodEnd = new Date(
                startOfPeriod.getTime() + 1000 * 60 * 60 * 24,
            );

            const actualPeriodEnd =
                currentPeriodEnd > dateTo ? new Date(dateTo) : currentPeriodEnd;

            const consumptions = await this.prisma.consumption.findMany({
                where: {
                    date: {
                        gte: currentPeriodStart,
                        lte: actualPeriodEnd,
                    },
                    userId,
                },
                include: {
                    items: {
                        include: {
                            food: true,
                        },
                    },
                },
            });

            const { totalCalories, totalProteins, totalFats, totalCarbs } =
                consumptions.reduce(
                    (totals, consumption) => {
                        consumption.items.forEach((item) => {
                            const { food, amount } = item;
                            totals.totalCalories +=
                                (food.caloriesPer100 * amount) / 100;
                            totals.totalProteins +=
                                (food.proteinsPer100 * amount) / 100;
                            totals.totalFats +=
                                (food.fatsPer100 * amount) / 100;
                            totals.totalCarbs +=
                                (food.carbsPer100 * amount) / 100;
                        });
                        return totals;
                    },
                    {
                        totalCalories: 0,
                        totalProteins: 0,
                        totalFats: 0,
                        totalCarbs: 0,
                    },
                );

            results.push({
                dateFrom: new Date(currentPeriodStart),
                dateTo: new Date(actualPeriodEnd),
                totalCalories,
                averageProteins: totalProteins,
                averageFats: totalFats,
                averageCarbs: totalCarbs,
            });

            startOfPeriod = currentPeriodEnd;
        }

        return results;
    }
}
