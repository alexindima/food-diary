import { TuiRoot } from '@taiga-ui/core';
import { Component, ViewEncapsulation } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './header/header.component';
import { MainComponent } from './main/main.component';
import { HeroComponent } from './hero/hero.component';
import { TranslateService } from '@ngx-translate/core';
import { FooterComponent } from './footer/footer.component';
import { AuthComponent } from './auth/auth.component';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterOutlet, TuiRoot, HeaderComponent, MainComponent, HeroComponent, TuiRoot, TuiRoot, FooterComponent, AuthComponent],
    templateUrl: './app.component.html',
    styleUrl: './app.component.less',
    encapsulation: ViewEncapsulation.None
})
export class AppComponent {
    public title = 'food-diary-web-client';
    public constructor(private translate: TranslateService) {
        translate.addLangs(['en', 'ru']);
        translate.setDefaultLang('en');

        const browserLang = translate.getBrowserLang();
        translate.use(browserLang?.match(/en|ru/) ? browserLang : 'en');
    }

    public switchLanguage(language: string): void {
        this.translate.use(language);
    }
}
