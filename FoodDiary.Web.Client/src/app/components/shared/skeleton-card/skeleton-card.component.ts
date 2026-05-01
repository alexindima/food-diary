import { ChangeDetectionStrategy, Component } from '@angular/core';

import { SkeletonComponent } from '../skeleton/skeleton.component';

@Component({
    selector: 'fd-skeleton-card',
    standalone: true,
    imports: [SkeletonComponent],
    templateUrl: './skeleton-card.component.html',
    styleUrl: './skeleton-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkeletonCardComponent {}
