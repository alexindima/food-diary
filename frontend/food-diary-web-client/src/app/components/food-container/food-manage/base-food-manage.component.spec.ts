import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BaseFoodManageComponent } from './base-food-manage.component';

describe('BaseFoodManageComponent', () => {
    let component: BaseFoodManageComponent;
    let fixture: ComponentFixture<BaseFoodManageComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [BaseFoodManageComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(BaseFoodManageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
