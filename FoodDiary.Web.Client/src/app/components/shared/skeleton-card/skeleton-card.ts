import { ChangeDetectionStrategy, Component } from '@angular/core';

import { SkeletonComponent } from '../skeleton/skeleton';

@Component({
    selector: 'fd-skeleton-card',
    imports: [SkeletonComponent],
    templateUrl: './skeleton-card.html',
    styleUrl: './skeleton-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkeletonCardComponent {}
