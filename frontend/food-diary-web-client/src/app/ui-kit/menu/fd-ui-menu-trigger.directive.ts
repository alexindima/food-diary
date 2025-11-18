import { Component } from '@angular/core';
import { MatMenuModule, MatMenuTrigger } from '@angular/material/menu';

@Component({
    selector: '[fdUiMenuTrigger]',
    standalone: true,
    template: `<ng-content></ng-content>`,
    hostDirectives: [
        {
            directive: MatMenuTrigger,
            inputs: ['matMenuTriggerFor: fdUiMenuTriggerFor'],
        },
    ],
    imports: [MatMenuModule],
})
export class FdUiMenuTriggerDirective {}
