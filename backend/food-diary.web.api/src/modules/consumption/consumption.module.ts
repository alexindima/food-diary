import { MiddlewareConsumer, Module } from '@nestjs/common';
import { ConsumptionController } from './controllers/consumption.controller';
import { ConsumptionService } from './services/consumption.service';
import { PrismaService } from 'prisma/prisma.service';
import { AuthMiddleware } from '../../middlewares/auth.middleware';
import { ConfigModule } from '@nestjs/config';
import { JwtModule } from '@nestjs/jwt';

@Module({
    imports: [ConfigModule, JwtModule.register({})],
    controllers: [ConsumptionController],
    providers: [ConsumptionService, PrismaService],
})
export class ConsumptionModule {
    configure(consumer: MiddlewareConsumer) {
        consumer.apply(AuthMiddleware).forRoutes(ConsumptionController);
    }
}
