import { Injectable } from '@nestjs/common';
import { User } from '@prisma/client';
import { JwtService } from '@nestjs/jwt';
import { PrismaService } from '../../../prisma/prisma.service';
import * as bcrypt from 'bcrypt';
import { UserDto } from '../../users/dto/user.dto';
import { LoginResponseDto } from '../dto/login-response.dto';

@Injectable()
export class AuthService {
    constructor(
        private readonly jwtService: JwtService,
        private readonly prisma: PrismaService,
    ) {}

    async register(email: string, password: string): Promise<LoginResponseDto> {
        const hashedPassword = await bcrypt.hash(password, 10);
        const user = await this.prisma.user.create({
            data: { email, password: hashedPassword },
        });

        return this.login(user);
    }

    async validateUser(email: string, password: string): Promise<User | null> {
        const user = await this.prisma.user.findUnique({ where: { email } });
        if (user && (await bcrypt.compare(password, user.password))) {
            return user;
        }
        return null;
    }

    async login(user: User): Promise<LoginResponseDto> {
        const payload = { email: user.email, sub: user.id };
        const access_token = this.jwtService.sign(payload);
        return {
            access_token,
            user: new UserDto(user),
        };
    }
}
