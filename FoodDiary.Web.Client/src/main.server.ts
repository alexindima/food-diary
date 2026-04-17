import { BootstrapContext, bootstrapApplication } from '@angular/platform-browser';
import { ApplicationRef } from '@angular/core';
import { AppComponent } from './app/shell/app.component';
import { config } from './app/app.config.server';

const bootstrap = (context: BootstrapContext): Promise<ApplicationRef> => bootstrapApplication(AppComponent, config, context);

export default bootstrap;
