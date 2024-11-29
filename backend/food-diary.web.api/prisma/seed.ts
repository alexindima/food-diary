import { PrismaClient, Unit } from '@prisma/client';
import * as bcrypt from 'bcrypt';
import * as dotenv from 'dotenv';

const envFile = `.env.${process.env.NODE_ENV || 'development'}`;
console.log(`Using environment file: ${envFile}`);
dotenv.config({ path: envFile });

const prisma = new PrismaClient();

async function main() {
    const user1 = await prisma.user.upsert({
        where: { email: 'user1@example.com' },
        update: {},
        create: {
            username: 'user1',
            email: 'user1@example.com',
            password: await bcrypt.hash('securepassword1', 10),
            firstName: 'John',
            lastName: 'Doe',
            isActive: true,
        },
    });

    const user2 = await prisma.user.upsert({
        where: { email: 'user2@example.com' },
        update: {},
        create: {
            username: 'user2',
            email: 'user2@example.com',
            password: await bcrypt.hash('securepassword2', 10),
            firstName: 'Jane',
            lastName: 'Smith',
            isActive: true,
        },
    });

    const user1Foods = [
        {
            name: 'Apple',
            category: 'Fruit',
            caloriesPer100: 52,
            proteinsPer100: 0.3,
            fatsPer100: 0.2,
            carbsPer100: 14,
            defaultServing: 100,
            defaultServingUnit: Unit.G,
            barcode: '1234567890123',
            userId: user1.id,
        },
        {
            name: 'Chicken Breast',
            category: 'Meat',
            caloriesPer100: 165,
            proteinsPer100: 31,
            fatsPer100: 3.6,
            carbsPer100: 0,
            defaultServing: 100,
            defaultServingUnit: Unit.G,
            barcode: '2345678901234',
            userId: user1.id,
        },
    ];

    const user2Foods = [
        {
            name: 'Banana',
            category: 'Fruit',
            caloriesPer100: 89,
            proteinsPer100: 1.1,
            fatsPer100: 0.3,
            carbsPer100: 23,
            defaultServing: 100,
            defaultServingUnit: Unit.G,
            barcode: '3456789012345',
            userId: user2.id,
        },
        {
            name: 'Salmon',
            category: 'Fish',
            caloriesPer100: 208,
            proteinsPer100: 20,
            fatsPer100: 13,
            carbsPer100: 0,
            defaultServing: 100,
            defaultServingUnit: Unit.G,
            barcode: '4567890123456',
            userId: user2.id,
        },
    ];

    const foods = await prisma.$transaction([
        ...user1Foods.map((food) => prisma.food.create({ data: food })),
        ...user2Foods.map((food) => prisma.food.create({ data: food })),
    ]);

    const createConsumptionForMonth = async (
        userId: number,
        foodIds: number[],
    ) => {
        const today = new Date();
        const daysInMonth = 30;
        const consumptions = [];
        const usageCountUpdates = {};

        for (let i = 0; i < daysInMonth; i++) {
            const date = new Date(today);
            date.setDate(today.getDate() - i);

            const dailyConsumptionCount = Math.floor(Math.random() * 5) + 1;

            for (let j = 0; j < dailyConsumptionCount; j++) {
                const foodId =
                    foodIds[Math.floor(Math.random() * foodIds.length)];
                const amount = Math.floor(Math.random() * 300) + 50;

                consumptions.push(
                    prisma.consumption.create({
                        data: {
                            userId,
                            date,
                            comment: `Random consumption for user ${userId}`,
                            items: {
                                create: [{ foodId, amount }],
                            },
                        },
                    }),
                );

                usageCountUpdates[foodId] =
                    (usageCountUpdates[foodId] || 0) + 1;
            }
        }

        return { consumptions, usageCountUpdates };
    };

    const user1Data = await createConsumptionForMonth(user1.id, [
        foods[0].id,
        foods[1].id,
    ]);
    const user2Data = await createConsumptionForMonth(user2.id, [
        foods[2].id,
        foods[3].id,
    ]);

    await prisma.$transaction([
        ...user1Data.consumptions,
        ...user2Data.consumptions,
        ...Object.entries(user1Data.usageCountUpdates).map(([foodId, count]) =>
            prisma.food.update({
                where: { id: Number(foodId) },
                data: { usageCount: { increment: count as number } },
            }),
        ),
        ...Object.entries(user2Data.usageCountUpdates).map(([foodId, count]) =>
            prisma.food.update({
                where: { id: Number(foodId) },
                data: { usageCount: { increment: count as number } },
            }),
        ),
    ]);

    console.log('Seeding completed successfully!');
}

main()
    .then(async () => {
        await prisma.$disconnect();
    })
    .catch(async (error) => {
        console.error(error);
        await prisma.$disconnect();
        process.exit(1);
    });
