import { Injectable } from '@angular/core';
import { Subject } from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class DropZoneService {
    private draggedElement: HTMLElement | null = null;
    private draggedData: any | null = null;

    private dragStartSubject = new Subject<any>();
    private dragUpdateSubject = new Subject<{ clientX: number; clientY: number }>();
    private dragEndSubject = new Subject<void>();

    dragStart$ = this.dragStartSubject.asObservable();
    dragUpdate$ = this.dragUpdateSubject.asObservable();
    dragEnd$ = this.dragEndSubject.asObservable();

    startDrag(data: any, element: HTMLElement): void {
        this.draggedData = data;
        this.draggedElement = element;
        this.dragStartSubject.next(data);
    }

    updateDrag(clientX: number, clientY: number): void {
        this.dragUpdateSubject.next({ clientX, clientY });
    }

    endDrag(): void {
        this.draggedData = null;
        this.draggedElement = null;
        this.dragEndSubject.next();
    }

    getDraggedElement(): HTMLElement | null {
        return this.draggedElement;
    }
}
