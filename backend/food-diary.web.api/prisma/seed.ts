import { PrismaClient, Unit } from '@prisma/client';
import * as bcrypt from 'bcrypt';

const prisma = new PrismaClient();

async function main() {
    const admin = await prisma.user.upsert({
        where: { email: 'admin@example.com' },
        update: {},
        create: {
            username: 'admin',
            email: 'admin@example.com',
            password: await bcrypt.hash('securepassword', 10),
            firstName: 'Admin',
            lastName: 'User',
            isActive: true,
            gender: 'M',
            weight: 70.0,
            height: 175.0,
        },
    });

    const adminGoal = await prisma.goal.create({
        data: {
            userId: admin.id,
            caloriesGoal: 2000,
            proteinsGoal: 150,
            fatsGoal: 70,
            carbsGoal: 250,
            startDate: new Date(),
            isActive: true,
        },
    });

    const foods = await prisma.food.createMany({
        data: [
            {
                name: 'Apple',
                category: 'Fruit',
                caloriesPer100: 52,
                proteinsPer100: 0.3,
                fatsPer100: 0.2,
                carbsPer100: 14,
                defaultServing: 100,
                defaultServingUnit: 'G',
            },
            {
                name: 'Banana',
                category: 'Fruit',
                caloriesPer100: 89,
                proteinsPer100: 1.1,
                fatsPer100: 0.3,
                carbsPer100: 23,
                defaultServing: 100,
                defaultServingUnit: Unit.G,
            },
            {
                name: 'Orange Juice',
                category: 'Drink',
                caloriesPer100: 45,
                proteinsPer100: 0.7,
                fatsPer100: 0.2,
                carbsPer100: 10,
                defaultServing: 200,
                defaultServingUnit: Unit.ML,
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
            },
        ],
    });

    const consumption = await prisma.consumption.create({
        data: {
            userId: admin.id,
            foodId: 1,
            amount: 150,
            date: new Date(),
        },
    });

    console.log({ admin, adminGoal, foods, consumption });
}

main()
    .then(async () => {
        await prisma.$disconnect();
    })
    .catch(async (e) => {
        console.error(e);
        await prisma.$disconnect();
        process.exit(1);
    });
