import { ChangeDetectionStrategy, Component, computed, effect, inject, input, model, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiAutocompleteComponent, type FdUiAutocompleteOption } from 'fd-ui-kit';
import { catchError, debounceTime, of, switchMap, tap } from 'rxjs';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { PublicRecipesFacade } from '../../lib/public-recipes.facade';

type CategoryChoice = { name: string; label: string };
const SEARCH_DELAY_MS = 250;

@Component({
    selector: 'fd-public-category-filter',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiAutocompleteComponent],
    templateUrl: './public-category-filter.html',
})
export class PublicCategoryFilterComponent {
    public readonly category = model('');
    public readonly language = input('');

    private readonly facade = inject(PublicRecipesFacade);
    private readonly translate = inject(TranslateService);
    private readonly uiLanguage = injectCurrentLanguage();
    protected readonly search = signal('');
    protected readonly loading = signal(true);
    protected readonly failed = signal(false);
    private readonly request = computed(() => ({ search: this.search(), language: this.language() }));
    private readonly categories = toSignal(
        toObservable(this.request).pipe(
            tap(() => {
                this.loading.set(true);
                this.failed.set(false);
            }),
            debounceTime(SEARCH_DELAY_MS),
            switchMap(query =>
                this.facade.getCategories(query.search, query.language !== '' ? query.language : undefined).pipe(
                    catchError(() => {
                        this.failed.set(true);
                        return of([] as string[]);
                    }),
                ),
            ),
            tap(() => {
                this.loading.set(false);
            }),
        ),
        { initialValue: [] as string[] },
    );
    protected readonly allCategory = computed(() => {
        this.uiLanguage();
        return { name: '', label: String(this.translate.instant('PUBLIC_RECIPES.ANY_CATEGORY')) };
    });
    protected readonly selected = computed(() =>
        this.category() !== '' ? { name: this.category(), label: this.category() } : this.allCategory(),
    );
    protected readonly options = computed<Array<FdUiAutocompleteOption<CategoryChoice>>>(() =>
        [this.allCategory(), ...this.categories().map(name => ({ name, label: name }))].map(value => ({ value, label: value.label })),
    );
    protected readonly displayCategory = (value: CategoryChoice | null): string => value?.label ?? '';
    public constructor() {
        effect(() => {
            this.category();
            this.language();
            this.search.set('');
        });
    }
    protected selectCategory(category: string): void {
        this.search.set('');
        this.category.set(category);
    }
    protected onValueChange(value: CategoryChoice | string | null): void {
        if (value === '' || value === null) {
            this.selectCategory('');
        }
    }
}
