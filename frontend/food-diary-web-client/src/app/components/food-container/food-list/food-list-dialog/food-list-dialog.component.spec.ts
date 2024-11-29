import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FoodListDialogComponent } from './food-list-dialog.component';

describe('AuthComponent', () => {
    let component: FoodListDialogComponent;
    let fixture: ComponentFixture<FoodListDialogComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FoodListDialogComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FoodListDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
