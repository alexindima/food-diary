import { Injectable } from '@nestjs/common';
import { UpdateUserDto } from '../dto/update-user.dto';
import { PrismaService } from '../../../../prisma/prisma.service';
import { User } from '@prisma/client';
import * as bcrypt from 'bcrypt';

@Injectable()
export class UserService {
    constructor(private readonly prismaService: PrismaService) {}

    async findById(id: number) {
        return this.prismaService.user.findUnique({
            where: { id },
        });
    }

    async findByEmail(email: string): Promise<User | null> {
        return this.prismaService.user.findUnique({ where: { email } });
    }

    async create(email: string, hashedPassword: string): Promise<User> {
        return this.prismaService.user.create({
            data: {
                email,
                password: hashedPassword,
            },
        });
    }

    async update(id: number, updateUserDto: UpdateUserDto) {
        if (updateUserDto.password) {
            updateUserDto.password = await bcrypt.hash(
                updateUserDto.password,
                10,
            );
        }

        return this.prismaService.user.update({
            where: { id },
            data: updateUserDto,
        });
    }

    async updateRefreshToken(
        id: number,
        hashedRefreshToken: string,
    ): Promise<void> {
        await this.prismaService.user.update({
            where: { id },
            data: { refreshToken: hashedRefreshToken },
        });
    }
}
