import type { ApplicationRef } from '@angular/core';
import { bootstrapApplication, type BootstrapContext } from '@angular/platform-browser';

import { config } from './app/app.config.server';
import { AppComponent } from './app/shell/app.component';

const bootstrap = (context: BootstrapContext): Promise<ApplicationRef> => bootstrapApplication(AppComponent, config, context);

export default bootstrap;
