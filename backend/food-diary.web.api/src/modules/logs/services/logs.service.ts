import { Injectable } from '@nestjs/common';
import { GlobalLogger } from '../../../common/logger.service';

@Injectable()
export class LogsService {
    constructor(private readonly logger: GlobalLogger) {}

    public log(message: string, level: LogLevel, metadata?: any): void {
        const enrichedMessage = `[Frontend] ${message}`;

        switch (level) {
            case 'info':
                this.logger.log(enrichedMessage);
                break;
            case 'error':
                this.logger.error(enrichedMessage, metadata?.trace);
                break;
            case 'warn':
                this.logger.warn(enrichedMessage);
                break;
            case 'debug':
                this.logger.debug(enrichedMessage);
                break;
            case 'verbose':
                this.logger.verbose(enrichedMessage);
                break;
        }
    }
}

export type LogLevel = 'info' | 'error' | 'warn' | 'debug' | 'verbose';
