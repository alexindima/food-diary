import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConsumptionAddComponent } from './consumption-add.component';

describe('FoodAddComponent', () => {
    let component: ConsumptionAddComponent;
    let fixture: ComponentFixture<ConsumptionAddComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ConsumptionAddComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(ConsumptionAddComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
