import { registerLocaleData } from '@angular/common';
import localeRu from '@angular/common/locales/ru';
import { bootstrapApplication } from '@angular/platform-browser';

import { appConfig } from './app/app.config';
import { AppComponent } from './app/shell/app.component';

registerLocaleData(localeRu);

bootstrapApplication(AppComponent, appConfig).catch(err => {
    // eslint-disable-next-line no-console -- Bootstrap failures happen before app logging is available.
    console.error(err);
});
