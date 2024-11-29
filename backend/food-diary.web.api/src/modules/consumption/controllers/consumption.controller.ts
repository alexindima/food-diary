import {
    Controller,
    Get,
    Query,
    Param,
    Post,
    Body,
    Patch,
    Delete,
    Req,
} from '@nestjs/common';
import { CreateConsumptionDto } from '../dto/create-consumption.dto';
import { ConsumptionService } from '../services/consumption.service';
import { ApiResponseDto } from '../../../dto/api-response.dto';
import { Consumption } from '@prisma/client';
import { PageOf } from '../../../interfaces/page-of.interface';
import { InvalidPaginationParamsException } from '../../../exceptions/invalid-pagination-params.exception';
import { ConsumptionQueryParams } from '../interfaces/consumption-query-params.interface';
import { UpdateConsumptionDto } from '../dto/update-consumption.dto';
import {
    ApiTags,
    ApiOperation,
    ApiResponse,
    ApiQuery,
    ApiParam,
} from '@nestjs/swagger';

@ApiTags('Consumptions')
@Controller('consumptions')
export class ConsumptionController {
    constructor(private readonly consumptionService: ConsumptionService) {}

    @Get()
    @ApiOperation({ summary: 'Query consumptions' })
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
        description: 'Returns a paginated list of consumptions',
    })
    @ApiResponse({ status: 400, description: 'Invalid pagination parameters' })
    async query(
        @Query() query: ConsumptionQueryParams,
        @Req() req: Request,
    ): Promise<ApiResponseDto<PageOf<Consumption>>> {
        const userId = req['userId'];
        const { page, limit, ...filters } = query;

        if (isNaN(Number(page)) || isNaN(Number(limit))) {
            throw new InvalidPaginationParamsException();
        }

        const result = await this.consumptionService.query(
            userId,
            page,
            limit,
            filters,
        );

        return ApiResponseDto.success(result);
    }

    @Get(':id')
    @ApiOperation({ summary: 'Get a consumption by ID' })
    @ApiParam({
        name: 'id',
        required: true,
        example: 1,
        description: 'Consumption ID',
    })
    @ApiResponse({ status: 200, description: 'Returns a consumption record' })
    @ApiResponse({ status: 404, description: 'Consumption not found' })
    async getById(@Param('id') id: string, @Req() req: Request) {
        const userId = req['userId'];
        const consumptionId = Number(id);

        const consumption = await this.consumptionService.getById(
            userId,
            consumptionId,
        );

        return ApiResponseDto.success(consumption);
    }

    @Post()
    @ApiOperation({ summary: 'Create a new consumption' })
    @ApiResponse({
        status: 201,
        description: 'Consumption successfully created',
    })
    @ApiResponse({ status: 400, description: 'Validation error' })
    async create(
        @Body() createConsumptionDto: CreateConsumptionDto,
        @Req() req: Request,
    ) {
        const userId = req['userId'];

        const newConsumption = await this.consumptionService.create(
            userId,
            createConsumptionDto,
        );

        return ApiResponseDto.success(newConsumption);
    }

    @Patch(':id')
    @ApiOperation({ summary: 'Update a consumption' })
    @ApiParam({
        name: 'id',
        required: true,
        example: 1,
        description: 'Consumption ID',
    })
    @ApiResponse({
        status: 200,
        description: 'Consumption successfully updated',
    })
    @ApiResponse({ status: 404, description: 'Consumption not found' })
    async update(
        @Param('id') id: string,
        @Body() updateConsumptionDto: UpdateConsumptionDto,
        @Req() req: Request,
    ) {
        const userId = req['userId'];
        const consumptionId = Number(id);

        const updatedConsumption = await this.consumptionService.update(
            userId,
            consumptionId,
            updateConsumptionDto,
        );

        return ApiResponseDto.success(updatedConsumption);
    }

    @Delete(':id')
    @ApiOperation({ summary: 'Delete a consumption' })
    @ApiParam({
        name: 'id',
        required: true,
        example: 1,
        description: 'Consumption ID',
    })
    @ApiResponse({
        status: 200,
        description: 'Consumption successfully deleted',
    })
    @ApiResponse({ status: 404, description: 'Consumption not found' })
    async delete(@Param('id') id: string, @Req() req: Request) {
        const userId = req['userId'];
        const consumptionId = Number(id);

        await this.consumptionService.delete(userId, consumptionId);

        return ApiResponseDto.success(null);
    }
}
