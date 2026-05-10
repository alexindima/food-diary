import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { CustomGroupComponent } from './custom-group.component';

describe('CustomGroupComponent', () => {
    let component: CustomGroupComponent;
    let fixture: ComponentFixture<CustomGroupComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [CustomGroupComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(CustomGroupComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('title', 'Group Title');
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should render title', () => {
        fixture.detectChanges();
        const el = fixture.nativeElement as HTMLElement;
        const titleEl = el.querySelector('.custom-group__title span');
        expect(titleEl?.textContent.trim()).toBe('Group Title');
    });

    it('should not render unused action controls', () => {
        fixture.detectChanges();
        const el = fixture.nativeElement as HTMLElement;

        expect(el.querySelector('.custom-group__toggle')).toBeNull();
        expect(el.querySelector('.custom-group__button')).toBeNull();
    });
});
