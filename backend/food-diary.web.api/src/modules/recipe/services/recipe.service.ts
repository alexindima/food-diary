import { Injectable } from '@nestjs/common';
import { PrismaService } from '../../../../prisma/prisma.service';
import { Prisma, Recipe } from '@prisma/client';
import { PageOf } from 'src/interfaces/page-of.interface';
import { CreateRecipeDto } from '../dto/create-recipe.dto';
import { RecipeQueryParams } from '../interfaces/recipe-query-params';

@Injectable()
export class RecipeService {
    constructor(private readonly prisma: PrismaService) {}

    // Получение списка рецептов с фильтрацией и пагинацией
    async query(
        userId: number,
        page: number = 1,
        limit: number = 25,
        filters: Partial<RecipeQueryParams> = {},
    ): Promise<PageOf<Recipe>> {
        const skip = (page - 1) * limit;
        const take = limit;

        const where: Prisma.RecipeWhereInput = {
            userId,
            ...filters,
        };

        const [data, totalItems] = await Promise.all([
            this.prisma.recipe.findMany({
                where,
                skip,
                take,
                orderBy: { createdAt: 'desc' },
                include: {
                    steps: {
                        include: {
                            ingredients: {
                                include: {
                                    food: true,
                                },
                            },
                        },
                    },
                },
            }),
            this.prisma.recipe.count({ where }),
        ]);

        return {
            data,
            page,
            limit,
            totalPages: Math.ceil(totalItems / limit),
            totalItems,
        };
    }

    // Получение рецепта по ID
    async getById(userId: number, id: number): Promise<Recipe | null> {
        return this.prisma.recipe.findUnique({
            where: { id, userId },
            include: {
                steps: {
                    include: {
                        ingredients: {
                            include: {
                                food: true,
                            },
                        },
                    },
                },
            },
        });
    }

    // Создание рецепта
    async create(
        userId: number,
        createRecipeDto: CreateRecipeDto,
    ): Promise<Recipe> {
        const { name, description, prepTime, cookTime, servings, steps } =
            createRecipeDto;

        return this.prisma.$transaction(async (prisma) => {
            const recipe = await prisma.recipe.create({
                data: {
                    userId,
                    name,
                    description,
                    prepTime,
                    cookTime,
                    servings,
                    steps: {
                        create: steps.map((step, index) => ({
                            stepNumber: index + 1,
                            description: step.description,
                            ingredients: {
                                create: step.ingredients.map((ingredient) => ({
                                    foodId: ingredient.foodId,
                                    amount: ingredient.amount,
                                })),
                            },
                        })),
                    },
                },
                include: {
                    steps: {
                        include: {
                            ingredients: {
                                include: {
                                    food: true,
                                },
                            },
                        },
                    },
                },
            });

            // Обновляем `usageCount` для каждого ингредиента
            for (const step of steps) {
                for (const ingredient of step.ingredients) {
                    await prisma.food.update({
                        where: { id: ingredient.foodId },
                        data: { usageCount: { increment: 1 } },
                    });
                }
            }

            return recipe;
        });
    }

    // Удаление рецепта
    async delete(userId: number, id: number): Promise<Recipe> {
        return this.prisma.$transaction(async (prisma) => {
            const recipe = await prisma.recipe.findUnique({
                where: { id, userId },
                include: {
                    steps: {
                        include: {
                            ingredients: {
                                include: {
                                    food: true,
                                },
                            },
                        },
                    },
                },
            });

            if (!recipe) {
                throw new Error(`Recipe with ID ${id} not found`);
            }

            for (const step of recipe.steps) {
                for (const ingredient of step.ingredients) {
                    await prisma.food.update({
                        where: { id: ingredient.foodId },
                        data: { usageCount: { decrement: 1 } },
                    });
                }
            }

            await prisma.recipeStep.deleteMany({
                where: { recipeId: id },
            });

            return prisma.recipe.delete({
                where: { id },
            });
        });
    }
}
