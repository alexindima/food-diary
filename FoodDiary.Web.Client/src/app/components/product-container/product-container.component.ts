import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'fd-product-container',
    templateUrl: './product-container.component.html',
    styleUrls: ['./product-container.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterOutlet],
})
export class ProductContainerComponent {}
