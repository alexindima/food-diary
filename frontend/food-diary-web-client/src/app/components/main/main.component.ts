import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeroComponent } from '../hero/hero.component';
import { FeaturesComponent } from '../features/features.component';

@Component({
    selector: 'app-main',
    standalone: true,
    imports: [RouterOutlet, HeroComponent, FeaturesComponent],
    templateUrl: './main.component.html',
    styleUrls: ['./main.component.less'],
})
export class MainComponent {}
