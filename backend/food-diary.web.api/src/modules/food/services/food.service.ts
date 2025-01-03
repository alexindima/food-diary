import { Injectable } from '@nestjs/common';
import { PrismaService } from '../../../../prisma/prisma.service';
import { CreateFoodDto } from '../dto/create-food.dto';
import { PageOf } from '../../../interfaces/page-of.interface';
import { Food, Prisma } from '@prisma/client';
import { FoodQueryParams } from '../interfaces/food-query-params.interface';
import { FoodInUseException } from '../../../exceptions/food-in-use.exception';
import { ValidationError } from 'class-validator';

@Injectable()
export class FoodService {
    constructor(private readonly prisma: PrismaService) {}

    async query(
        page: number = 1,
        limit: number = 25,
        filters: Partial<FoodQueryParams>,
    ): Promise<PageOf<Food>> {
        const skip = (Number(page) - 1) * Number(limit);
        const take = Number(limit);

        const where: FoodFilter = {};

        if (filters.search) {
            where.OR = [
                { name: { contains: filters.search, mode: 'insensitive' } },
                { barcode: { contains: filters.search, mode: 'insensitive' } },
            ];
        }

        const [data, totalItems] = await Promise.all([
            this.prisma.food.findMany({
                where,
                skip,
                take,
            }),
            this.prisma.food.count({ where }),
        ]);

        return {
            data,
            page,
            limit,
            totalPages: Math.ceil(totalItems / limit),
            totalItems,
        };
    }

    async getById(id: number) {
        return this.prisma.food.findUnique({ where: { id } });
    }

    async create(createFoodDto: CreateFoodDto, userId: number) {
        return this.prisma.food.create({
            data: {
                ...createFoodDto,
                userId,
            },
        });
    }

    async update(id: number, data: CreateFoodDto) {
        const food = await this.prisma.food.findUnique({ where: { id } });
        if (food && food.usageCount > 0) {
            throw new FoodInUseException();
        }

        if (data.proteinsPerBase + data.carbsPerBase + data.fatsPerBase === 0) {
            throw new ValidationError();
        }

        return this.prisma.food.update({
            where: { id },
            data,
        });
    }

    async delete(id: number) {
        const food = await this.prisma.food.findUnique({ where: { id } });
        if (food && food.usageCount > 0) {
            throw new FoodInUseException();
        }

        return this.prisma.food.delete({
            where: { id },
        });
    }
}

type FoodFilter = Prisma.FoodWhereInput & {
    OR?: Prisma.FoodWhereInput[];
};
