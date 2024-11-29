import { MiddlewareConsumer, Module, RequestMethod } from '@nestjs/common';
import { PrismaService } from 'prisma/prisma.service';
import { StatisticsService } from './services/statistics.service';
import { StatisticsController } from './controllers/statistics.controller';
import { ConfigModule } from '@nestjs/config';
import { JwtModule } from '@nestjs/jwt';
import { AuthMiddleware } from '../../middlewares/auth.middleware';

@Module({
    imports: [ConfigModule, JwtModule.register({})],
    controllers: [StatisticsController],
    providers: [StatisticsService, PrismaService],
})
export class StatisticsModule {
    configure(consumer: MiddlewareConsumer) {
        consumer.apply(AuthMiddleware).forRoutes(StatisticsController);
    }
}
