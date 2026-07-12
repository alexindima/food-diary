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
