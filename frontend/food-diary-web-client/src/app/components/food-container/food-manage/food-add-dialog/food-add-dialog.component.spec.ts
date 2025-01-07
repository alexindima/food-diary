import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FoodAddDialogComponent } from './food-add-dialog.component';

describe('FoodAddComponent', () => {
    let component: FoodAddDialogComponent;
    let fixture: ComponentFixture<FoodAddDialogComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FoodAddDialogComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FoodAddDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
