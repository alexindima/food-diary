import { Injectable } from '@nestjs/common';
import { PrismaService } from '../../../../prisma/prisma.service';
import { CreateConsumptionDto } from '../dto/create-consumption.dto';
import { PageOf } from '../../../interfaces/page-of.interface';
import { Consumption, Prisma } from '@prisma/client';
import { ConsumptionQueryParams } from '../interfaces/consumption-query-params.interface';
import { UpdateConsumptionDto } from '../dto/update-consumption.dto';

@Injectable()
export class ConsumptionService {
    constructor(private readonly prisma: PrismaService) {}

    async query(
        userId: number,
        page: number = 1,
        limit: number = 25,
        filters: Partial<ConsumptionQueryParams>,
    ): Promise<PageOf<Consumption>> {
        const skip = (Number(page) - 1) * Number(limit);
        const take = Number(limit);

        const where: ConsumptionFilter = { userId };

        if (filters.dateFrom || filters.dateTo) {
            where.date = {};
            if (filters.dateFrom) {
                where.date.gte = new Date(filters.dateFrom);
            }
            if (filters.dateTo) {
                where.date.lte = new Date(filters.dateTo);
            }
        }

        const [data, totalItems] = await Promise.all([
            this.prisma.consumption.findMany({
                where,
                skip,
                take,
                orderBy: { date: 'desc' },
                include: { items: { include: { food: true } } },
            }),
            this.prisma.consumption.count({ where }),
        ]);

        return {
            data,
            page,
            limit,
            totalPages: Math.ceil(totalItems / limit),
            totalItems,
        };
    }

    async getById(userId: number, id: number) {
        return this.prisma.consumption.findUnique({
            where: { userId, id },
            include: { items: { include: { food: true } } },
        });
    }

    async create(userId: number, createConsumptionDto: CreateConsumptionDto) {
        const { date, items, comment } = createConsumptionDto;

        return this.prisma.$transaction(async (prisma) => {
            const consumption = await prisma.consumption.create({
                data: {
                    user: { connect: { id: userId } },
                    date,
                    comment,
                    items: {
                        create: items.map((item) => ({
                            foodId: item.foodId,
                            amount: item.amount,
                        })),
                    },
                },
                include: { items: { include: { food: true } } },
            });

            for (const item of items) {
                await prisma.food.update({
                    where: { id: item.foodId },
                    data: { usageCount: { increment: 1 } },
                });
            }

            return consumption;
        });
    }

    async update(
        userId: number,
        id: number,
        updateConsumptionDto: UpdateConsumptionDto,
    ) {
        const { items, ...data } = updateConsumptionDto;

        return this.prisma.$transaction(async (prisma) => {
            const oldConsumption = await prisma.consumption.findUnique({
                where: { id, userId },
                include: { items: true },
            });

            if (!oldConsumption) {
                throw new Error(`Consumption with ID ${id} not found`);
            }

            for (const oldItem of oldConsumption.items) {
                await prisma.food.update({
                    where: { id: oldItem.foodId },
                    data: { usageCount: { decrement: 1 } },
                });
            }

            const updatedConsumption = await prisma.consumption.update({
                where: { id },
                data: {
                    ...data,
                    items: items
                        ? {
                              deleteMany: {},
                              create: items.map((item) => ({
                                  foodId: item.foodId,
                                  amount: item.amount,
                              })),
                          }
                        : undefined,
                },
                include: { items: { include: { food: true } } },
            });

            for (const newItem of items || []) {
                await prisma.food.update({
                    where: { id: newItem.foodId },
                    data: { usageCount: { increment: 1 } },
                });
            }

            return updatedConsumption;
        });
    }

    async delete(userId: number, id: number): Promise<Consumption> {
        return this.prisma.$transaction(async (prisma) => {
            const consumption = await prisma.consumption.findUnique({
                where: { id, userId },
                include: { items: true },
            });

            if (!consumption) {
                throw new Error(
                    `Consumption with userId ${userId} and ID ${id} not found`,
                );
            }

            for (const item of consumption.items) {
                await prisma.food.update({
                    where: { id: item.foodId },
                    data: { usageCount: { decrement: 1 } },
                });
            }

            await prisma.consumptionItem.deleteMany({
                where: { consumptionId: id },
            });

            return prisma.consumption.delete({
                where: { id },
            });
        });
    }
}

type ConsumptionFilter = Prisma.ConsumptionWhereInput & {
    OR?: Prisma.ConsumptionWhereInput[];
};
