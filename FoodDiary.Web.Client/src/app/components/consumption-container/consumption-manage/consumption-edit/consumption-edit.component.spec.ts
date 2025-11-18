import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConsumptionEditComponent } from './consumption-edit.component';

describe('ConsumptionEditComponent', () => {
    let component: ConsumptionEditComponent;
    let fixture: ComponentFixture<ConsumptionEditComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ConsumptionEditComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(ConsumptionEditComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
