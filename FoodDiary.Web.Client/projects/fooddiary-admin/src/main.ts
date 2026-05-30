import { bootstrapApplication } from '@angular/platform-browser';

import { AppComponent } from './app/app';
import { appConfig } from './app/app.config';

bootstrapApplication(AppComponent, appConfig).catch(error => {
    // eslint-disable-next-line no-console -- Bootstrap failures happen before app logging is available.
    console.error(error);
});
