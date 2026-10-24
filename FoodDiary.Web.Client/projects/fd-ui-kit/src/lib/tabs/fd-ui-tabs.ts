import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    type ElementRef,
    inject,
    input,
    model,
    signal,
    viewChildren,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

export type FdUiTab = {
    value: string;
    label?: string;
    labelKey?: string;
};

export type FdUiTabsAppearance = 'default' | 'wrap-compact';

@Component({
    selector: 'fd-ui-tabs',
    imports: [RouterLink],
    templateUrl: './fd-ui-tabs.html',
    styleUrls: ['./fd-ui-tabs.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiTabsComponent {
    private static nextId = 0;
    private readonly destroyRef = inject(DestroyRef);
    private readonly translateService = inject(TranslateService);
    private readonly languageVersion = signal(0);

    public readonly tabs = input<FdUiTab[]>([]);
    public readonly selectedValue = model.required<string>();
    public readonly appearance = input<FdUiTabsAppearance>('default');
    public readonly mode = input<'tabs' | 'selection' | 'navigation'>('tabs');
    public readonly ariaLabel = input<string | null>(null);
    public readonly ariaLabelledBy = input<string | null>(null);
    protected readonly tabButtons = viewChildren<ElementRef<HTMLButtonElement>>('tabButton');
    protected readonly tabsId = `fd-ui-tabs-${FdUiTabsComponent.nextId++}`;
    protected readonly listRole = computed(() =>
        this.mode() === 'tabs' ? 'tablist' : this.mode() === 'navigation' ? 'navigation' : 'group',
    );

    protected readonly appearanceClass = computed(() => `fd-ui-tabs--appearance-${this.appearance()}`);
    protected readonly selectedIndex = computed(() => {
        const index = this.tabs().findIndex(tab => tab.value === this.selectedValue());
        return Math.max(index, 0);
    });
    protected readonly tabItems = computed<FdUiTabViewModel[]>(() => {
        this.languageVersion();

        const mode = this.mode();
        const selectedIndex = this.selectedIndex();
        return this.tabs().map((tab, index) => ({
            ...tab,
            labelText: tab.label ?? (tab.labelKey !== undefined ? this.translateService.instant(tab.labelKey) : ''),
            selected: index === selectedIndex,
            role: mode === 'tabs' ? 'tab' : null,
            ariaSelected: mode === 'tabs' ? index === selectedIndex : null,
            ariaPressed: mode === 'selection' ? index === selectedIndex : null,
            controls: mode === 'tabs' ? this.panelId(tab.value) : null,
            tabIndex: mode !== 'tabs' || index === selectedIndex ? 0 : -1,
        }));
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
    }

    protected selectIndex(index: number): void {
        const tab = this.tabs().at(index);
        if (tab === undefined) {
            return;
        }
        this.selectedValue.set(tab.value);
    }

    protected onKeydown(index: number, event: KeyboardEvent): void {
        if (this.mode() !== 'tabs') {
            return;
        }
        const tabs = this.tabs();
        if (tabs.length === 0) {
            return;
        }

        let nextIndex: number | null = null;

        switch (event.key) {
            case 'ArrowRight': {
                nextIndex = (index + 1) % tabs.length;
                break;
            }
            case 'ArrowLeft': {
                nextIndex = (index - 1 + tabs.length) % tabs.length;
                break;
            }
            case 'Home': {
                nextIndex = 0;
                break;
            }
            case 'End': {
                nextIndex = tabs.length - 1;
                break;
            }
            case 'Enter':
            case ' ': {
                event.preventDefault();
                this.selectIndex(index);
                return;
            }
            default: {
                return;
            }
        }

        event.preventDefault();
        this.focusTab(nextIndex);
        this.selectIndex(nextIndex);
    }

    // eslint-disable-next-line local/prefer-protected-template-members -- Consumers associate their panels with these public IDs.
    public tabId(value: string): string {
        return `${this.tabsId}-tab-${encodeURIComponent(value)}`;
    }

    // eslint-disable-next-line local/prefer-protected-template-members -- Consumers associate their panels with these public IDs.
    public panelId(value: string): string {
        return `${this.tabsId}-panel-${encodeURIComponent(value)}`;
    }

    private focusTab(index: number): void {
        this.tabButtons()[index]?.nativeElement.focus();
    }
}

type FdUiTabViewModel = {
    labelText: string;
    selected: boolean;
    role: 'tab' | null;
    ariaSelected: boolean | null;
    ariaPressed: boolean | null;
    controls: string | null;
    tabIndex: number;
} & FdUiTab;
