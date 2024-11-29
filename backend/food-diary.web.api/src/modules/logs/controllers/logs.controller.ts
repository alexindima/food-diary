import { Body, Controller, Post } from '@nestjs/common';
import { ApiTags, ApiOperation, ApiBody } from '@nestjs/swagger';
import { LogLevel, LogsService } from '../services/logs.service';

@ApiTags('Logs')
@Controller('logs')
export class LogsController {
    constructor(private readonly logsService: LogsService) {}

    @Post()
    @ApiOperation({
        summary: 'Create a new log',
        description: 'Logs a message with a specific level and optional trace.',
    })
    @ApiBody({
        description: 'Log data to be sent to the backend',
        schema: {
            type: 'object',
            properties: {
                message: {
                    type: 'string',
                    description: 'The log message',
                    example: 'An error occurred',
                },
                level: {
                    type: 'string',
                    description: 'The level of the log',
                    example: 'error',
                    enum: ['info', 'error', 'warn', 'debug', 'verbose'],
                },
                trace: {
                    type: 'string',
                    description: 'Optional trace information',
                    example: 'Stack trace here...',
                },
            },
        },
    })
    public async createLog(
        @Body() logData: { message: string; level: LogLevel; trace?: string },
    ): Promise<void> {
        const { message, level, trace } = logData;
        this.logsService.log(message, level, { trace });
    }
}
