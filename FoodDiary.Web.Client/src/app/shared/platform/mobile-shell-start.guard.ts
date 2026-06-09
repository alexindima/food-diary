import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';

import { isMobileShellWindow } from './mobile-shell-runtime';

export const mobileShellStartGuard: CanActivateFn = () => {
    if (!isMobileShellRuntime()) {
        return true;
    }

    return inject(Router).createUrlTree(['/mobile/login']);
};

function isMobileShellRuntime(): boolean {
    if (!isPlatformBrowser(inject(PLATFORM_ID))) {
        return false;
    }

    const windowRef = getWindowRef();
    if (windowRef === null) {
        return false;
    }

    return isMobileShellWindow(windowRef);
}

function getWindowRef(): Window | null {
    return inject(DOCUMENT).defaultView;
}
