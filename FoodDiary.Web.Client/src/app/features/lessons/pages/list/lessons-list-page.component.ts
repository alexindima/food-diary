import { ChangeDetectionStrategy, Component, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { FdCardHoverDirective } from '../../../../directives/card-hover.directive';
import { LessonFacade } from '../../lib/lesson.facade';
import { LESSON_CATEGORIES } from '../../models/lesson.data';

@Component({
    selector: 'fd-lessons-list-page',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiIconModule,
        FdUiLoaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        FdCardHoverDirective,
    ],
    providers: [LessonFacade],
    templateUrl: './lessons-list-page.component.html',
    styleUrl: './lessons-list-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonsListPageComponent implements OnInit {
    private readonly router = inject(Router);
    public readonly facade = inject(LessonFacade);
    public readonly categories = LESSON_CATEGORIES;

    public readonly progress = computed(() => {
        const all = this.facade.lessons();
        if (all.length === 0) {
            return null;
        }
        const read = all.filter(l => l.isRead).length;
        return { read, total: all.length, percent: Math.round((read / all.length) * 100) };
    });

    public ngOnInit(): void {
        this.facade.loadLessons();
    }

    public filterByCategory(category: string | null): void {
        this.facade.categoryFilter.set(category);
        this.facade.loadLessons(category);
    }

    public openLesson(id: string): void {
        void this.router.navigate(['/lessons', id]);
    }
}
