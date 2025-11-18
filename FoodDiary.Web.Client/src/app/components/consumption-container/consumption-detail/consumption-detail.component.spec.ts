import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ConsumptionDetailComponent } from './consumption-detail.component';

describe('ConsumptionDetailComponent', () => {
    let component: ConsumptionDetailComponent;
    let fixture: ComponentFixture<ConsumptionDetailComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ConsumptionDetailComponent],
            providers: [
                {
                    provide: MAT_DIALOG_DATA,
                    useValue: {
                        id: 1,
                        date: new Date().toISOString(),
                        items: [],
                    },
                },
                {
                    provide: MatDialogRef,
                    useValue: jasmine.createSpyObj('MatDialogRef', ['close']),
                },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(ConsumptionDetailComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
