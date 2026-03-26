import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DropZoneService {
    private draggedElement: HTMLElement | null = null;
    private draggedData: unknown = null;

    private readonly dragStartSubject = new Subject<unknown>();
    private dragUpdateSubject = new Subject<{ clientX: number; clientY: number }>();
    private dragEndSubject = new Subject<void>();

    public readonly dragStart$ = this.dragStartSubject.asObservable();
    public readonly dragUpdate$ = this.dragUpdateSubject.asObservable();
    public readonly dragEnd$ = this.dragEndSubject.asObservable();

    public startDrag(data: unknown, element: HTMLElement): void {
        this.draggedData = data;
        this.draggedElement = element;
        this.dragStartSubject.next(data);
    }

    public updateDrag(clientX: number, clientY: number): void {
        this.dragUpdateSubject.next({ clientX, clientY });
    }

    public endDrag(): void {
        this.draggedData = null;
        this.draggedElement = null;
        this.dragEndSubject.next();
    }

    public getDraggedElement(): HTMLElement | null {
        return this.draggedElement;
    }
}
