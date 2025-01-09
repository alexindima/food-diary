import { Component, input } from '@angular/core';

@Component({
  selector: 'fd-custom-group',
  imports: [],
  templateUrl: './custom-group.component.html',
  styleUrl: './custom-group.component.less'
})
export class CustomGroupComponent {
    public title = input.required<string>()
}
