import { Body, Controller, Post } from '@nestjs/common';
import { AuthService } from '../services/auth.service';
import { LoginUserDto } from '../dto/login-user.dto';
import { InvalidRefreshTokenException } from '../../../exceptions/invalid-refresh-token.exception';
import { ApiTags, ApiOperation, ApiResponse, ApiBody } from '@nestjs/swagger';
import { ApiResponseDto } from '../../../dto/api-response.dto';
import { CreateUserDto } from '../../user/dto/create-user.dto';

@ApiTags('Authentication')
@Controller('auth')
export class AuthController {
    constructor(private readonly authService: AuthService) {}

    @Post('register')
    @ApiOperation({ summary: 'User registration' })
    @ApiResponse({ status: 201, description: 'User successfully registered.' })
    @ApiResponse({
        status: 400,
        description: 'Validation error or registration failed.',
    })
    async register(@Body() createUserDto: CreateUserDto) {
        const result = await this.authService.register(
            createUserDto.email,
            createUserDto.password,
        );

        return ApiResponseDto.success(result);
    }

    @Post('login')
    @ApiOperation({ summary: 'User login' })
    @ApiResponse({ status: 200, description: 'Login successful.' })
    @ApiResponse({ status: 401, description: 'Invalid credentials.' })
    async login(@Body() loginUserDto: LoginUserDto) {
        const result = await this.authService.login(loginUserDto);

        return ApiResponseDto.success(result);
    }

    @Post('refresh')
    @ApiOperation({ summary: 'Token refresh' })
    @ApiBody({
        description: 'Refresh token',
        schema: { type: 'string', example: 'refresh_token_example' },
    })
    @ApiResponse({ status: 200, description: 'Token successfully refreshed.' })
    @ApiResponse({ status: 401, description: 'Invalid refresh token.' })
    async refresh(@Body('refreshToken') refreshToken: string) {
        try {
            return await this.authService.refreshToken(refreshToken);
        } catch (error) {
            throw new InvalidRefreshTokenException();
        }
    }
}
