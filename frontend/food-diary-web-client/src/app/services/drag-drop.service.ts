import { Injectable } from '@angular/core';
import { DropZoneDirective } from "../directives/drop-zone.directive";

@Injectable({ providedIn: 'root' })
export class DragDropService {
    private dropZones: DropZoneDirective[] = [];
    private activeDropZone: DropZoneDirective | null = null;

    /**
     * Регистрирует дроп-зону.
     */
    public registerDropZone(zone: DropZoneDirective): void {
        this.dropZones.push(zone);
    }

    /**
     * Удаляет дроп-зону.
     */
    public unregisterDropZone(zone: DropZoneDirective): void {
        this.dropZones = this.dropZones.filter((z) => z !== zone);
    }

    /**
     * Возвращает все зарегистрированные дроп-зоны.
     */
    public getDropZones(): DropZoneDirective[] {
        return this.dropZones;
    }

    /**
     * Устанавливает активную дроп-зону.
     */
    public setActiveDropZone(zone: DropZoneDirective | null): void {
        this.activeDropZone = zone;
    }

    /**
     * Возвращает активную дроп-зону.
     */
    public getActiveDropZone(): DropZoneDirective | null {
        return this.activeDropZone;
    }
}
