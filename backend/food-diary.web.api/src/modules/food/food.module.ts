import { MiddlewareConsumer, Module, RequestMethod } from '@nestjs/common';
import { FoodController } from './controllers/food.controller';
import { FoodService } from './services/food.service';
import { PrismaService } from 'prisma/prisma.service';
import { ConfigModule } from '@nestjs/config';
import { JwtModule } from '@nestjs/jwt';
import { AuthMiddleware } from '../../middlewares/auth.middleware';

@Module({
    imports: [ConfigModule, JwtModule.register({})],
    controllers: [FoodController],
    providers: [FoodService, PrismaService],
})
export class FoodModule {
    configure(consumer: MiddlewareConsumer) {
        consumer
            .apply(AuthMiddleware)
            .forRoutes({ path: 'foods/', method: RequestMethod.POST });
    }
}
