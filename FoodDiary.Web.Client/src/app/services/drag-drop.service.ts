import { ElementRef, Injectable } from '@angular/core';
import { DropZoneDirective } from '../directives/drop-zone.directive';

@Injectable({
    providedIn: 'root'
})
export class DragDropService {
    public dropZones = new Set<DropZoneDirective>();

    public registerDropZone(zone: DropZoneDirective): void {
        this.dropZones.add(zone);
    }

    public unregisterDropZone(zone: DropZoneDirective): void {
        this.dropZones.delete(zone);
    }

    public findDropZone(element: ElementRef): DropZoneDirective {
        for (const zone of this.dropZones) {
            if (zone.elementRef.nativeElement.contains(element.nativeElement)) {
                return zone;
            }
        }

        throw new Error(
            'Drop zone not found! Make sure every element with [fdDraggable] is inside an element with [fdDropZone].'
        );
    }
}
