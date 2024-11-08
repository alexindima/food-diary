import {
    Body,
    Controller,
    Post,
    Req,
    UnauthorizedException,
    UseGuards,
} from '@nestjs/common';
import { AuthService } from '../services/auth.service';
import { JwtAuthGuard } from '../guards/jwt-auth.guard';
import { AuthRequest } from '../interfaces/auth-request.interface';
import { CreateUserDto } from '../dto/create-user.dto';
import { LoginUserDto } from '../dto/login-user.dto';

@Controller('auth')
export class AuthController {
    constructor(private readonly authService: AuthService) {}

    @Post('register')
    async register(@Body() createUserDto: CreateUserDto) {
        return this.authService.register(
            createUserDto.email,
            createUserDto.password,
        );
    }

    @Post('login')
    async login(@Body() loginUserDto: LoginUserDto) {
        const user = await this.authService.validateUser(
            loginUserDto.email,
            loginUserDto.password,
        );
        if (!user) {
            throw new UnauthorizedException();
        }
        return this.authService.login(user);
    }

    @UseGuards(JwtAuthGuard)
    @Post('profile')
    getProfile(@Req() req: AuthRequest) {
        return req.user;
    }
}
