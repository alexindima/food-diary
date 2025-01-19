import { MiddlewareConsumer, Module } from '@nestjs/common';
import { PrismaService } from '../../../prisma/prisma.service';
import { ConfigModule } from '@nestjs/config';
import { JwtModule } from '@nestjs/jwt';
import { AuthMiddleware } from '../../middlewares/auth.middleware';
import { RecipeController } from './controllers/recipe.controller';
import { RecipeService } from './services/recipe.service';

@Module({
    imports: [ConfigModule, JwtModule.register({})],
    controllers: [RecipeController],
    providers: [RecipeService, PrismaService],
})
export class RecipeModule {
    configure(consumer: MiddlewareConsumer) {
        consumer.apply(AuthMiddleware).forRoutes(RecipeController);
    }
}
