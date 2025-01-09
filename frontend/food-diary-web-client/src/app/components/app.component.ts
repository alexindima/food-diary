import { TuiRoot } from '@taiga-ui/core';
import { Component, ViewEncapsulation } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './header/header.component';
import { FooterComponent } from './footer/footer.component';

@Component({
    selector: 'fd-root',
    imports: [RouterOutlet, TuiRoot, HeaderComponent, TuiRoot, TuiRoot, FooterComponent],
    templateUrl: './app.component.html',
    styleUrl: './app.component.less',
    encapsulation: ViewEncapsulation.None
})
export class AppComponent {
    public title = 'food-diary-web-client';
}
