import { Service } from '@angular/core';
import { WebmcpScreenRegistry } from 'fd-webmcp';

import { DevelopmentToolError } from '../../shared/development/development-tools';

/** This application's optional screen host; feature capabilities remain app-owned. */
@Service()
export class LocalWebmcpScreensService extends WebmcpScreenRegistry {
    public constructor() {
        super(message => new DevelopmentToolError(message));
    }
}
