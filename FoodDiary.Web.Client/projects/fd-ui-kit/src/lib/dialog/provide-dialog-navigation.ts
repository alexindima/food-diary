import { APP_BASE_HREF, type LocationChangeListener, LocationStrategy, PathLocationStrategy, PlatformLocation } from '@angular/common';
import { type EnvironmentProviders, inject, makeEnvironmentProviders } from '@angular/core';

import { FD_UI_DIALOG_HISTORY } from './fd-ui-dialog-history';

class DialogLocationStrategy extends PathLocationStrategy {
    private readonly dialogHistory = inject(FD_UI_DIALOG_HISTORY);

    public constructor() {
        super(inject(PlatformLocation), inject(APP_BASE_HREF, { optional: true }) ?? undefined);
    }

    public override onPopState(listener: LocationChangeListener): void {
        // Intercept before Location notifies Router and CDK. Window listener ordering is not sufficient.
        super.onPopState(event => {
            if (!this.dialogHistory.handlePopState(event)) {
                listener(event);
            }
        });
    }

    public override pushState(state: unknown, title: string, url: string, queryParams: string): void {
        super.pushState(state, title, url, queryParams);
        this.dialogHistory.recordPagePush();
    }
}

/** Keeps path-based page navigation behind the topmost modal's dismissal. */
export function provideFdUiDialogNavigation(): EnvironmentProviders {
    return makeEnvironmentProviders([{ provide: LocationStrategy, useFactory: (): LocationStrategy => new DialogLocationStrategy() }]);
}
