import {
    Body,
    Controller,
    Delete,
    Get,
    Param,
    Patch,
    Post,
    Query,
    Req,
    UseGuards,
} from '@nestjs/common';
import { FoodService } from '../services/food.service';
import { CreateFoodDto } from '../dto/create-food.dto';
import { UpdateFoodDto } from '../dto/update-food.dto';
import { PageOf } from '../../../interfaces/page-of.interface';
import { FoodQueryParams } from '../interfaces/food-query-params.interface';
import { ApiResponseDto } from '../../../dto/api-response.dto';
import { InvalidIdFormatException } from '../../../exceptions/invalid-id-format.exception';
import { InvalidPaginationParamsException } from '../../../exceptions/invalid-pagination-params.exception';
import { Food } from '@prisma/client';
import { JwtAuthGuard } from '../../auth/guards/jwt-auth.guard';
import {
    ApiTags,
    ApiOperation,
    ApiResponse,
    ApiQuery,
    ApiParam,
} from '@nestjs/swagger';

@ApiTags('Foods')
@Controller('foods')
export class FoodController {
    constructor(private readonly foodService: FoodService) {}

    @Get()
    @UseGuards(JwtAuthGuard)
    @ApiOperation({ summary: 'Query foods' })
    @ApiQuery({
        name: 'page',
        required: true,
        example: 1,
        description: 'Page number',
    })
    @ApiQuery({
        name: 'limit',
        required: true,
        example: 10,
        description: 'Items per page',
    })
    @ApiQuery({
        name: 'filters',
        required: false,
        description: 'Additional query filters',
    })
    @ApiResponse({
        status: 200,
        description: 'Returns a paginated list of foods',
    })
    @ApiResponse({ status: 400, description: 'Invalid pagination parameters' })
    async query(
        @Query() query: FoodQueryParams,
    ): Promise<ApiResponseDto<PageOf<Food>>> {
        const { page, limit, ...filters } = query;
        if (isNaN(Number(page)) || isNaN(Number(limit))) {
            throw new InvalidPaginationParamsException();
        }
        const result = await this.foodService.query(page, limit, filters);
        return ApiResponseDto.success(result);
    }

    @Get(':id')
    @UseGuards(JwtAuthGuard)
    @ApiOperation({ summary: 'Get a food by ID' })
    @ApiParam({
        name: 'id',
        required: true,
        example: 1,
        description: 'Food ID',
    })
    @ApiResponse({ status: 200, description: 'Returns a food record' })
    @ApiResponse({ status: 404, description: 'Food not found' })
    async getById(@Param('id') id: string) {
        const foodId = Number(id);
        if (isNaN(foodId)) {
            throw new InvalidIdFormatException();
        }
        const food = await this.foodService.getById(foodId);
        return ApiResponseDto.success(food);
    }

    @Post()
    @ApiOperation({ summary: 'Create a new food' })
    @ApiResponse({ status: 201, description: 'Food successfully created' })
    @ApiResponse({ status: 400, description: 'Validation error' })
    async create(@Body() createFoodDto: CreateFoodDto, @Req() req: Request) {
        const userId = req['userId'];
        const food = await this.foodService.create(createFoodDto, userId);
        return ApiResponseDto.success(food);
    }

    @Patch(':id')
    @UseGuards(JwtAuthGuard)
    @ApiOperation({ summary: 'Update a food' })
    @ApiParam({
        name: 'id',
        required: true,
        example: 1,
        description: 'Food ID',
    })
    @ApiResponse({ status: 200, description: 'Food successfully updated' })
    @ApiResponse({ status: 404, description: 'Food not found' })
    async update(
        @Param('id') id: string,
        @Body() updateFoodDto: UpdateFoodDto,
    ) {
        const foodId = Number(id);
        if (isNaN(foodId)) {
            throw new InvalidIdFormatException();
        }
        const updatedFood = await this.foodService.update(
            foodId,
            updateFoodDto,
        );
        return ApiResponseDto.success(updatedFood);
    }

    @Delete(':id')
    @UseGuards(JwtAuthGuard)
    @ApiOperation({ summary: 'Delete a food' })
    @ApiParam({
        name: 'id',
        required: true,
        example: 1,
        description: 'Food ID',
    })
    @ApiResponse({ status: 200, description: 'Food successfully deleted' })
    @ApiResponse({ status: 404, description: 'Food not found' })
    async delete(@Param('id') id: string) {
        const foodId = Number(id);
        await this.foodService.delete(foodId);
        return ApiResponseDto.success(null);
    }
}
