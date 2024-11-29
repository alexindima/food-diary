import { MiddlewareConsumer, Module } from '@nestjs/common';
import { UserService } from './services/user.service';
import { UsersController } from './controllers/users.controller';
import { PrismaService } from '../../../prisma/prisma.service';
import { ConfigModule } from '@nestjs/config';
import { JwtModule } from '@nestjs/jwt';
import { AuthMiddleware } from '../../middlewares/auth.middleware';

@Module({
    imports: [ConfigModule, JwtModule.register({})],
    controllers: [UsersController],
    providers: [UserService, PrismaService],
})
export class UserModule {
    configure(consumer: MiddlewareConsumer) {
        consumer.apply(AuthMiddleware).forRoutes(UsersController);
    }
}
