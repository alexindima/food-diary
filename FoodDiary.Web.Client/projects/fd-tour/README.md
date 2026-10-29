# fd-tour

Reusable guided-tour engine for FoodDiary Angular applications.

## Usage

Render the host once near the application shell:

```html
<router-outlet /> <fd-tour-host />
```

Mark targets with the directive:

```html
<button fdTourTarget="dashboard-ai-input">Add meal</button>
```

Start a tour from app code:

```ts
import { inject } from '@angular/core';
import { FdTourService, type FdTourDefinition } from 'fd-tour';

const tour: FdTourDefinition = {
    id: 'dashboard-welcome',
    version: 1,
    steps: [
        {
            id: 'ai-input',
            target: '[data-tour-id="dashboard-ai-input"]',
            title: 'Describe a meal',
            description: 'Use natural language to add food faster.',
            placement: 'bottom',
        },
    ],
};

inject(FdTourService).start(tour);
```

App-specific tour definitions and translated copy should stay in the consuming application.

Targets accept CSS selector lists. The engine scrolls to and highlights the first
visible match. Containers can register a `FdTourTargetResolver` with
`FdTourTargetRegistry` to reveal hidden targets, such as actions inside a responsive
menu. `canReveal(selector)` checks the current layout and target availability;
`reveal(selector)` updates container state and returns a cleanup callback. The
engine waits for Angular to render before finding, scrolling to and presenting the
actual target. Reveal must not activate the action itself.

Components with `display: contents` resolve to their first visible rendered child;
empty containers are omitted. Where supported, the host uses a manual native
popover to keep navigation above menus in the browser top layer. Other browsers
retain the fixed overlay.

Keep the same `data-tour-id` on desktop controls and their mobile counterparts.
Register a resolver at its container boundary and call the returned disposer on
destruction. Cleanup runs when leaving the step, closing, skipping, finishing or
replacing the tour, and when the engine is destroyed. A disappearing resolver
closes the active tour. `refreshTarget()` rechecks the active target on viewport
changes. Targets with neither a visible match nor a resolver are skipped; failed
reveals are skipped after rendering. Tour versions and completion storage are
unchanged.

While a responsive view replaces DOM nodes, the active step is retained and
resolved again after rendering. Structural changes are observed only while a
tour is active. Navigation still skips unavailable steps.
