import { Injectable } from '@angular/core';
import { DropZoneDirective } from '../directives/drop-zone.directive';

@Injectable({ providedIn: 'root' })
export class DragDropService {
    private dropZones: DropZoneDirective[] = [];
    private activeDropZone: DropZoneDirective | null = null;

    public registerDropZone(zone: DropZoneDirective): void {
        if (this.dropZones.includes(zone)) {
            console.warn('Drop zone is already registered:', zone);
            return;
        }

        this.dropZones.push(zone);
    }

    public unregisterDropZone(zone: DropZoneDirective): void {
        this.dropZones = this.dropZones.filter((z) => z !== zone);
    }

    public getDropZones(): DropZoneDirective[] {
        return this.dropZones;
    }

    public setActiveDropZone(zone: DropZoneDirective | null): void {
        this.activeDropZone = zone;
    }

    public getActiveDropZone(): DropZoneDirective | null {
        return this.activeDropZone;
    }
}
