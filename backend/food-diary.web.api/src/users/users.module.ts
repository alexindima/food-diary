import { Module } from '@nestjs/common';
import { UsersService } from './services/users.service';
import { UsersController } from './controllers/users.controller';
import { PrismaService } from '../../prisma/prisma.service';

@Module({
    controllers: [UsersController],
    providers: [UsersService, PrismaService],
})
export class UsersModule {}
