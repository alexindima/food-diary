import { Directive, input, output } from '@angular/core';

import type { DashboardBlockState } from './dashboard-view.types';

@Directive({
    selector: '[fdDashboardBlockHost]',
    host: {
        '[class.dashboard__block--hidden]': 'state().hidden',
        '[attr.role]': 'state().role',
        '[attr.tabindex]': 'state().tabIndex',
        '[attr.aria-pressed]': 'state().ariaPressed',
        '[attr.aria-disabled]': 'state().ariaDisabled',
        '[attr.aria-label]': 'state().ariaLabel',
        '(click)': 'activate($event)',
        '(keydown.enter)': 'activate($event)',
        '(keydown.space)': 'activateWithSpace($event)',
    },
})
export class DashboardBlockHostDirective {
    public readonly state = input.required<DashboardBlockState>();

    public readonly blockActivated = output<Event>();

    protected activate(event: Event): void {
        this.blockActivated.emit(event);
    }

    protected activateWithSpace(event: Event): void {
        event.preventDefault();
        this.activate(event);
    }
}

@Directive({
    selector: '[fdDashboardBlockContent]',
    host: {
        '[attr.inert]': 'state().inert',
        '[attr.aria-hidden]': 'state().inert === null ? null : true',
    },
})
export class DashboardBlockContentDirective {
    public readonly state = input.required<DashboardBlockState>();
}
