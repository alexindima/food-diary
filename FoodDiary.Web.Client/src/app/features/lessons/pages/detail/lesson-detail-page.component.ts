import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { LessonFacade } from '../../lib/lesson.facade';

@Component({
    selector: 'fd-lesson-detail-page',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiIconModule,
        FdUiLoaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
    ],
    providers: [LessonFacade],
    templateUrl: './lesson-detail-page.component.html',
    styleUrl: './lesson-detail-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonDetailPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    public readonly facade = inject(LessonFacade);

    public constructor() {
        const id = this.route.snapshot.paramMap.get('id');
        if (id) {
            this.facade.loadLesson(id);
        }
    }

    public markRead(): void {
        const lesson = this.facade.selectedLesson();
        if (lesson && !lesson.isRead) {
            this.facade.markRead(lesson.id);
        }
    }

    public goBack(): void {
        void this.router.navigate(['/lessons']);
    }
}
