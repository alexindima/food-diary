import { DOCUMENT } from '@angular/common';
import { booleanAttribute, Directive, effect, inject, input, Renderer2 } from '@angular/core';

@Directive({
    selector: '[fdUiPreventUnload]',
})
export class FdUiPreventUnloadDirective {
    public readonly fdUiPreventUnload = input(false, { transform: booleanAttribute });

    private readonly document = inject(DOCUMENT);
    private readonly renderer = inject(Renderer2);

    public constructor() {
        effect(onCleanup => {
            if (!this.fdUiPreventUnload() || this.document.defaultView === null) {
                return;
            }
            const stop = this.renderer.listen('window', 'beforeunload', (event: BeforeUnloadEvent) => {
                if (this.fdUiPreventUnload()) {
                    event.preventDefault();
                }
            });
            onCleanup(stop);
        });
    }
}
