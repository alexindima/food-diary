import {
    Controller,
    Get,
    Post,
    Body,
    Patch,
    Req,
    NotFoundException,
} from '@nestjs/common';
import { UserService } from '../services/user.service';
import { CreateUserDto } from '../dto/create-user.dto';
import { UpdateUserDto } from '../dto/update-user.dto';
import { UserDto } from '../dto/user.dto';
import { ApiResponseDto } from '../../../dto/api-response.dto';
import { ApiTags, ApiOperation, ApiResponse, ApiBody } from '@nestjs/swagger';

@ApiTags('Users')
@Controller('users')
export class UsersController {
    constructor(private readonly usersService: UserService) {}

    @Get('info')
    @ApiOperation({ summary: 'Get user information' })
    @ApiResponse({
        status: 200,
        description: 'User information retrieved successfully',
        type: UserDto,
    })
    @ApiResponse({ status: 404, description: 'User not found' })
    async getInfo(@Req() req: Request): Promise<ApiResponseDto<UserDto>> {
        const userId = req['userId'];

        const user = await this.usersService.findById(+userId);
        if (!user) {
            throw new NotFoundException('User not found');
        }
        const userDto = new UserDto(user);

        return ApiResponseDto.success(userDto);
    }

    @Post()
    @ApiOperation({ summary: 'Create a new user' })
    @ApiBody({ description: 'User creation data', type: CreateUserDto })
    @ApiResponse({
        status: 201,
        description: 'User successfully created',
        type: UserDto,
    })
    @ApiResponse({ status: 400, description: 'Validation error' })
    async create(@Body() createUserDto: CreateUserDto) {
        const user = await this.usersService.create(
            createUserDto.email,
            createUserDto.password,
        );

        return ApiResponseDto.success(new UserDto(user));
    }

    @Patch()
    @ApiOperation({ summary: 'Update user information' })
    @ApiBody({ description: 'User update data', type: UpdateUserDto })
    @ApiResponse({
        status: 200,
        description: 'User successfully updated',
        type: UserDto,
    })
    @ApiResponse({ status: 400, description: 'Validation error' })
    async update(@Req() req: Request, @Body() updateUserDto: UpdateUserDto) {
        const userId = req['userId'];

        const updatedUser = await this.usersService.update(
            +userId,
            updateUserDto,
        );

        return ApiResponseDto.success(new UserDto(updatedUser));
    }
}
