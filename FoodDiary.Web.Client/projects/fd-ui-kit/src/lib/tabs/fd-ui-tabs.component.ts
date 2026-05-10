import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    type ElementRef,
    inject,
    input,
    model,
    output,
    signal,
    viewChildren,
    ViewEncapsulation,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

export interface FdUiTab {
    value: string;
    label?: string;
    labelKey?: string;
}

export type FdUiTabsAppearance = 'default' | 'wrap-compact';

@Component({
    selector: 'fd-ui-tabs',
    standalone: true,
    imports: [TranslateModule],
    templateUrl: './fd-ui-tabs.component.html',
    styleUrls: ['./fd-ui-tabs.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
})
export class FdUiTabsComponent {
    private static nextId = 0;
    private readonly destroyRef = inject(DestroyRef);
    private readonly translateService = inject(TranslateService);
    private readonly languageVersion = signal(0);

    public readonly tabs = input<FdUiTab[]>([]);
    public readonly selectedValue = model.required<string>();
    public readonly selectedValueChange = output<string>();
    public readonly appearance = input<FdUiTabsAppearance>('default');
    protected readonly tabButtons = viewChildren<ElementRef<HTMLButtonElement>>('tabButton');
    protected readonly tabsId = `fd-ui-tabs-${FdUiTabsComponent.nextId++}`;

    protected readonly appearanceClass = computed(() => `fd-ui-tabs--appearance-${this.appearance()}`);
    protected readonly selectedIndex = computed(() => {
        const index = this.tabs().findIndex(tab => tab.value === this.selectedValue());
        return index >= 0 ? index : 0;
    });
    protected readonly tabItems = computed<FdUiTabViewModel[]>(() => {
        this.languageVersion();

        return this.tabs().map(tab => ({
            ...tab,
            labelText: tab.label ?? (tab.labelKey ? this.translateService.instant(tab.labelKey) : ''),
        }));
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
    }

    protected selectIndex(index: number): void {
        const tab = this.tabs().at(index);
        if (!tab) {
            return;
        }
        this.selectedValue.set(tab.value);
        this.selectedValueChange.emit(tab.value);
    }

    protected onKeydown(index: number, event: KeyboardEvent): void {
        const tabs = this.tabs();
        if (!tabs.length) {
            return;
        }

        let nextIndex: number | null = null;

        switch (event.key) {
            case 'ArrowRight':
            case 'ArrowDown':
                nextIndex = (index + 1) % tabs.length;
                break;
            case 'ArrowLeft':
            case 'ArrowUp':
                nextIndex = (index - 1 + tabs.length) % tabs.length;
                break;
            case 'Home':
                nextIndex = 0;
                break;
            case 'End':
                nextIndex = tabs.length - 1;
                break;
            case 'Enter':
            case ' ':
                event.preventDefault();
                this.selectIndex(index);
                return;
            default:
                return;
        }

        event.preventDefault();
        this.focusTab(nextIndex);
        this.selectIndex(nextIndex);
    }

    protected tabId(index: number): string {
        return `${this.tabsId}-tab-${index}`;
    }

    private focusTab(index: number): void {
        this.tabButtons()[index]?.nativeElement.focus();
    }
}

interface FdUiTabViewModel extends FdUiTab {
    labelText: string;
}
