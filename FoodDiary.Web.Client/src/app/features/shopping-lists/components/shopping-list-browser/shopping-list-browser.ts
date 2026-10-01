import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    effect,
    ElementRef,
    inject,
    input,
    output,
    signal,
} from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent, FdUiInputComponent } from 'fd-ui-kit';

import { ShoppingListBrowserFacade } from '../../lib/shopping-list-browser.facade';
import type { ShoppingListPage, ShoppingListSummary } from '../../models/shopping-list.data';

@Component({
    selector: 'fd-shopping-list-browser',
    providers: [ShoppingListBrowserFacade],
    templateUrl: './shopping-list-browser.html',
    styleUrl: './shopping-list-browser.scss',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent, FdUiInputComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingListBrowserComponent {
    protected readonly browser = inject(ShoppingListBrowserFacade);
    private readonly translate = inject(TranslateService);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    public readonly initialPage = input<ShoppingListPage | null>(null);
    public readonly lists = input.required<readonly ShoppingListSummary[]>();
    public readonly selectedId = input<string | null>(null);
    public readonly focusSearch = input(true);
    public readonly createRequested = output<string | void>();
    protected readonly maxNameLength = 128;
    protected readonly createName = computed(() =>
        !this.browser.loading() && !this.browser.failed() && this.matches().length === 0 ? String(this.search() ?? '').trim() : '',
    );
    public readonly listSelected = output<string>();
    protected readonly search = signal<string | number | null>('');
    protected readonly completedOpen = signal(false);
    protected readonly query = computed(() =>
        String(this.search() ?? '')
            .trim()
            .toLocaleLowerCase(),
    );
    protected readonly matches = computed(() => {
        const current = new Map(this.lists().map(list => [list.id, list]));
        return this.browser.lists().map(list => {
            const latest = current.get(list.id) ?? list;
            return { ...latest, completed: latest.itemsCount > 0 && latest.remainingCount === 0 };
        });
    });
    protected readonly completed = computed(() => this.matches().filter(list => list.completed));
    protected readonly visible = computed(() => this.matches().filter(list => this.query().length > 0 || !list.completed));
    protected status(list: ShoppingListSummary): string {
        if (list.completed === true) {
            return this.translate.instant('SHOPPING_LIST.DONE_STATUS');
        }
        return list.itemsCount === 0 ? this.translate.instant('SHOPPING_LIST.EMPTY_LIST') : String(list.remainingCount ?? '…');
    }
    protected onScroll(element: HTMLElement): void {
        const threshold = 80;
        if (element.scrollHeight - element.scrollTop - element.clientHeight < threshold && !this.browser.failed()) {
            this.browser.loadMore();
        }
    }
    public constructor() {
        effect(onCleanup => {
            const query = this.query();
            const page = this.initialPage();
            if (page !== null && query.length === 0) {
                this.browser.seed(page);
                return;
            }
            this.browser.cancelSearch();
            const delay = 300;
            const timer = setTimeout(() => {
                this.browser.reset(query);
            }, delay);
            onCleanup(() => {
                clearTimeout(timer);
            });
        });
        afterNextRender(() => {
            if (this.focusSearch()) {
                this.host.nativeElement.querySelector<HTMLInputElement>('input')?.focus();
            }
        });
    }
}
