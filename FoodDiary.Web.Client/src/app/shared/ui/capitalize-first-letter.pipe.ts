import { Pipe, type PipeTransform } from '@angular/core';

import { capitalizeFirstLetter } from '../lib/text-display.utils';

@Pipe({
    name: 'capitalizeFirstLetter',
})
export class CapitalizeFirstLetterPipe implements PipeTransform {
    public transform(value: string | null | undefined): string {
        return capitalizeFirstLetter(value);
    }
}
