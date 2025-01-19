import {
    Controller,
    Get,
    Post,
    Body,
    Patch,
    Req,
    Query,
    Param,
    Delete,
} from '@nestjs/common';
import { ApiResponseDto } from '../../../dto/api-response.dto';
import {
    ApiTags,
    ApiOperation,
    ApiResponse,
    ApiQuery,
    ApiParam,
} from '@nestjs/swagger';
import { PageOf } from '../../../interfaces/page-of.interface';
import { Recipe } from '@prisma/client';
import { InvalidPaginationParamsException } from '../../../exceptions/invalid-pagination-params.exception';
import { CreateRecipeDto } from '../dto/create-recipe.dto';
import { RecipeService } from '../services/recipe.service';

@ApiTags('Recipes')
@Controller('recipes')
export class RecipeController {
    constructor(private readonly recipeService: RecipeService) {}

    @Get()
    @ApiOperation({ summary: 'Query recipes' })
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
        description: 'Additional filters',
    })
    @ApiResponse({
        status: 200,
        description: 'Returns a paginated list of recipes',
    })
    @ApiResponse({ status: 400, description: 'Invalid pagination parameters' })
    async query(
        @Query() query: any, // Можно создать интерфейс для фильтров, если требуется
        @Req() req: Request,
    ): Promise<ApiResponseDto<PageOf<Recipe>>> {
        const userId = req['userId'];
        const { page, limit, ...filters } = query;

        if (isNaN(Number(page)) || isNaN(Number(limit))) {
            throw new InvalidPaginationParamsException();
        }

        const result = await this.recipeService.query(
            userId,
            page,
            limit,
            filters,
        );

        return ApiResponseDto.success(result);
    }

    @Get(':id')
    @ApiOperation({ summary: 'Get a recipe by ID' })
    @ApiParam({
        name: 'id',
        required: true,
        example: 1,
        description: 'Recipe ID',
    })
    @ApiResponse({ status: 200, description: 'Returns a recipe record' })
    @ApiResponse({ status: 404, description: 'Recipe not found' })
    async getById(@Param('id') id: string, @Req() req: Request) {
        const userId = req['userId'];
        const recipeId = Number(id);

        const recipe = await this.recipeService.getById(userId, recipeId);

        return ApiResponseDto.success(recipe);
    }

    @Post()
    @ApiOperation({ summary: 'Create a new recipe' })
    @ApiResponse({
        status: 201,
        description: 'Recipe successfully created',
    })
    @ApiResponse({ status: 400, description: 'Validation error' })
    async create(
        @Body() createRecipeDto: CreateRecipeDto,
        @Req() req: Request,
    ) {
        const userId = req['userId'];

        const newRecipe = await this.recipeService.create(
            userId,
            createRecipeDto,
        );

        return ApiResponseDto.success(newRecipe);
    }

    /*@Patch(':id')
    @ApiOperation({ summary: 'Update a recipe' })
    @ApiParam({
        name: 'id',
        required: true,
        example: 1,
        description: 'Recipe ID',
    })
    @ApiResponse({
        status: 200,
        description: 'Recipe successfully updated',
    })
    @ApiResponse({ status: 404, description: 'Recipe not found' })
    async update(
        @Param('id') id: string,
        @Body() updateRecipeDto: UpdateRecipeDto,
        @Req() req: Request,
    ) {
        const userId = req['userId'];
        const recipeId = Number(id);

        const updatedRecipe = await this.recipeService.update(
            userId,
            recipeId,
            updateRecipeDto,
        );

        return ApiResponseDto.success(updatedRecipe);
    }*/

    @Delete(':id')
    @ApiOperation({ summary: 'Delete a recipe' })
    @ApiParam({
        name: 'id',
        required: true,
        example: 1,
        description: 'Recipe ID',
    })
    @ApiResponse({
        status: 200,
        description: 'Recipe successfully deleted',
    })
    @ApiResponse({ status: 404, description: 'Recipe not found' })
    async delete(@Param('id') id: string, @Req() req: Request) {
        const userId = req['userId'];
        const recipeId = Number(id);

        await this.recipeService.delete(userId, recipeId);

        return ApiResponseDto.success(null);
    }
}
