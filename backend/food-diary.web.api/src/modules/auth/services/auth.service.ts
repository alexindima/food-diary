import { Injectable } from '@nestjs/common';
import { User } from '@prisma/client';
import { JwtService } from '@nestjs/jwt';
import * as bcrypt from 'bcrypt';
import { LoginResponseDto } from '../dto/login-response.dto';
import { LoginUserDto } from '../dto/login-user.dto';
import { UserExistsException } from '../../../exceptions/user.exists.exception';
import { InvalidCredentialsException } from '../../../exceptions/invalid-credentials.exception';
import { UserService } from '../../user/services/user.service';
import { UserDto } from '../../user/dto/user.dto';
import { InvalidRefreshTokenException } from '../../../exceptions/invalid-refresh-token.exception';

@Injectable()
export class AuthService {
    constructor(
        private readonly jwtService: JwtService,
        private readonly userService: UserService,
    ) {}

    public async register(
        email: string,
        password: string,
    ): Promise<LoginResponseDto> {
        const existingUser = await this.userService.findByEmail(email);
        if (existingUser) {
            throw new UserExistsException();
        }

        const hashedPassword = await bcrypt.hash(password, 10);
        const newUser = await this.userService.create(email, hashedPassword);

        return await this.createLoginResponse(newUser);
    }

    public async validateUser(
        email: string,
        password: string,
    ): Promise<User | null> {
        const user = await this.userService.findByEmail(email);
        if (user && (await bcrypt.compare(password, user.password))) {
            return user;
        }
        return null;
    }

    public async login(loginUserDto: LoginUserDto): Promise<LoginResponseDto> {
        const user = await this.validateUser(
            loginUserDto.email,
            loginUserDto.password,
        );
        if (!user) {
            throw new InvalidCredentialsException();
        }
        return await this.createLoginResponse(user);
    }

    public async refreshToken(
        refreshToken: string,
    ): Promise<{ accessToken: string }> {
        const payload = this.jwtService.verify(refreshToken, {
            secret: process.env.JWT_REFRESH_SECRET,
        });

        const user = await this.userService.findById(payload.sub);
        if (!user || !user.refreshToken) {
            throw new InvalidRefreshTokenException();
        }

        const isTokenValid = await bcrypt.compare(
            refreshToken,
            user.refreshToken,
        );
        if (!isTokenValid) {
            throw new InvalidRefreshTokenException();
        }

        const newAccessToken = this.jwtService.sign(
            { email: payload.email, sub: payload.sub },
            {
                secret: process.env.JWT_SECRET,
                expiresIn: '15m',
            },
        );

        return { accessToken: newAccessToken };
    }

    private async createLoginResponse(user: User): Promise<LoginResponseDto> {
        const payload = { email: user.email, sub: user.id };

        const accessToken = this.jwtService.sign(payload, {
            secret: process.env.JWT_SECRET,
            expiresIn: '15m',
        });

        const refreshToken = this.jwtService.sign(payload, {
            secret: process.env.JWT_REFRESH_SECRET,
            expiresIn: '7d',
        });

        const hashedRefreshToken = await bcrypt.hash(refreshToken, 10);
        await this.userService.updateRefreshToken(user.id, hashedRefreshToken);

        return {
            accessToken,
            refreshToken,
            user: new UserDto(user),
        };
    }
}
