import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MatPaginator } from '@angular/material/paginator';
import { By } from '@angular/platform-browser';
import { FdUiPaginationComponent } from './fd-ui-pagination.component';

describe('FdUiPaginationComponent', () => {
    let fixture: ComponentFixture<FdUiPaginationComponent>;
    let component: FdUiPaginationComponent;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiPaginationComponent],
            providers: [provideNoopAnimations()],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiPaginationComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should set length on paginator', () => {
        fixture.componentRef.setInput('length', 50);
        fixture.detectChanges();

        const paginator = fixture.debugElement.query(By.directive(MatPaginator));
        expect(paginator.componentInstance.length).toBe(50);
    });

    it('should set pageSize on paginator', () => {
        fixture.componentRef.setInput('pageSize', 25);
        fixture.detectChanges();

        const paginator = fixture.debugElement.query(By.directive(MatPaginator));
        expect(paginator.componentInstance.pageSize).toBe(25);
    });

    it('should set pageIndex on paginator', () => {
        fixture.componentRef.setInput('length', 100);
        fixture.componentRef.setInput('pageSize', 10);
        fixture.componentRef.setInput('pageIndex', 3);
        fixture.detectChanges();

        const paginator = fixture.debugElement.query(By.directive(MatPaginator));
        expect(paginator.componentInstance.pageIndex).toBe(3);
    });
});
