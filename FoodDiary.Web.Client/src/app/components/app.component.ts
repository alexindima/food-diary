import { Component, ViewEncapsulation } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './header/header.component';
import { FooterComponent } from './footer/footer.component';
import { FdLayoutPageDirective } from '../directives/layout/page-layout.directive';

@Component({
    selector: 'fd-root',
    imports: [RouterOutlet, HeaderComponent, FooterComponent, FdLayoutPageDirective],
    templateUrl: './app.component.html',
    styleUrl: './app.component.less',
    encapsulation: ViewEncapsulation.None
})
export class AppComponent {
    public title = 'food-diary-web-client';
}
