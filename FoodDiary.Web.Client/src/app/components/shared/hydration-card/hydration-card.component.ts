import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { NoticeBannerComponent } from '../notice-banner/notice-banner.component';

@Component({
    selector: 'fd-hydration-card',
    standalone: true,
    imports: [CommonModule, FdUiButtonComponent, TranslatePipe, NoticeBannerComponent],
    templateUrl: './hydration-card.component.html',
    styleUrl: './hydration-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HydrationCardComponent {
    public readonly title = input<string>('Hydration');
    public readonly total = input<number>(0);
    public readonly goal = input<number | null>(null);
    public readonly addStep = input<number>(250);
    public readonly isLoading = input<boolean>(false);
    public readonly canAdd = input<boolean>(true);
    public readonly addClick = output<number>();
    public readonly goalAction = output<void>();

    public readonly hasGoal = computed(() => !!this.goal() && this.goal()! > 0);
    public readonly percent = computed(() => {
        if (!this.hasGoal()) return 0;
        const value = (this.total() / (this.goal() ?? 1)) * 100;
        return Math.max(0, Math.min(value, 200)); // allow slight overflow visualization
    });

    public readonly trackWidth = computed(() => `${Math.min(this.percent(), 130)}%`);

    public onAdd(): void {
        if (!this.canAdd()) {
            return;
        }
        const step = Math.max(1, this.addStep());
        this.addClick.emit(step);
    }

    public onGoalAction(): void {
        this.goalAction.emit();
    }
}
