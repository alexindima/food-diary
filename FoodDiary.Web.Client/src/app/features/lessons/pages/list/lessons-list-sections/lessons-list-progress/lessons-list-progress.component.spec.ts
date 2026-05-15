import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import type { LessonProgressViewModel } from '../../../../lib/lesson-view.mapper';
import { LessonsListProgressComponent } from './lessons-list-progress.component';

describe('LessonsListProgressComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [LessonsListProgressComponent, TranslateModule.forRoot()],
        });
    });

    it('renders progress when available', () => {
        const fixture = createComponent({ read: 2, total: 4, percent: 50 });
        const element = getElement(fixture);

        expect(element.querySelector('.lessons-list__progress-fill')?.getAttribute('style')).toContain('width: 50%');
        expect(element.textContent).toContain('2/4');
    });

    it('renders nothing when progress is missing', () => {
        const fixture = createComponent(null);

        expect(getElement(fixture).querySelector('.lessons-list__progress')).toBeNull();
    });
});

function createComponent(progress: LessonProgressViewModel | null): ComponentFixture<LessonsListProgressComponent> {
    const fixture = TestBed.createComponent(LessonsListProgressComponent);
    fixture.componentRef.setInput('progress', progress);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<LessonsListProgressComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}
