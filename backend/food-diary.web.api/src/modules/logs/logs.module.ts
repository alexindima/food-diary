import { Module } from '@nestjs/common';
import { LogsService } from './services/logs.service';
import { LogsController } from './controllers/logs.controller';
import { GlobalLogger } from '../../common/logger.service';

@Module({
    controllers: [LogsController],
    providers: [LogsService, GlobalLogger],
})
export class LogsModule {}
