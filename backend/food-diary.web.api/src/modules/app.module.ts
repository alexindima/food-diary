import { Module } from '@nestjs/common';
import { AuthModule } from './auth/auth.module';
import { ConfigModule } from '@nestjs/config';
import { UserModule } from './user/user.module';
import { FoodModule } from './food/food.module';
import { ConsumptionModule } from './consumption/consumption.module';
import { StatisticsModule } from './statistics/statistics.module';
import { LogsModule } from './logs/logs.module';
import { RecipeModule } from "./recipe/recipe.module";

@Module({
    imports: [
        AuthModule,
        ConfigModule.forRoot({
            isGlobal: true,
        }),
        ConsumptionModule,
        FoodModule,
        LogsModule,
        RecipeModule,
        StatisticsModule,
        UserModule,
    ],
    controllers: [],
})
export class AppModule {}
