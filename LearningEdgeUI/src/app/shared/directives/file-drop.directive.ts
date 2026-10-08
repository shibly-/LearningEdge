import { Directive, output, signal } from '@angular/core';

/**
 * Drag-and-drop file source. Pair it with a real <input type="file"> so the
 * feature stays keyboard accessible — a drop zone alone is not (spec 7, spec 9).
 */
@Directive({
  selector: '[appFileDrop]',
  host: {
    '[class.is-dragging]': 'isDragging()',
    '(dragenter)': 'onDragEnter($event)',
    '(dragover)': 'onDragOver($event)',
    '(dragleave)': 'onDragLeave($event)',
    '(drop)': 'onDrop($event)',
  },
})
export class FileDropDirective {
  readonly filesDropped = output<readonly File[]>();

  /**
   * dragenter/dragleave fire for every child element crossed, so a boolean
   * flickers. Counting them keeps the state steady until the pointer leaves.
   */
  private depth = 0;
  private readonly dragging = signal(false);
  readonly isDragging = this.dragging.asReadonly();

  onDragEnter(event: DragEvent): void {
    event.preventDefault();
    this.depth++;
    this.dragging.set(true);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.depth = Math.max(0, this.depth - 1);
    if (this.depth === 0) {
      this.dragging.set(false);
    }
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.depth = 0;
    this.dragging.set(false);

    const files = event.dataTransfer?.files;
    if (files === undefined || files.length === 0) {
      return;
    }
    this.filesDropped.emit(Array.from(files));
  }
}
