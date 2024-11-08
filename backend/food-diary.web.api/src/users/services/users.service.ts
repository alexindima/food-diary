import { Injectable } from '@nestjs/common';
import { CreateUserDto } from '../../auth/dto/create-user.dto';
import { UpdateUserDto } from '../dto/update-user.dto';
import { PrismaService } from '../../../prisma/prisma.service';

@Injectable()
export class UsersService {
    constructor(private readonly prismaService: PrismaService) {}

    async create(createUserDto: CreateUserDto) {
        return this.prismaService.user.create({
            data: createUserDto,
        });
    }

    async findAll() {
        return this.prismaService.user.findMany();
    }

    async findOne(id: number) {
        return this.prismaService.user.findUnique({
            where: { id },
        });
    }

    async update(id: number, updateUserDto: UpdateUserDto) {
        return this.prismaService.user.update({
            where: { id },
            data: updateUserDto,
        });
    }

    async remove(id: number) {
        return this.prismaService.user.delete({
            where: { id },
        });
    }
}
