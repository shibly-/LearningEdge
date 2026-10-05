import { Directive, output, signal } from '@angular/core';
import { HostListener } from '@angular/core';

/**
 * Drag-and-drop file source. Pair it with a real <input type="file"> so the
 * feature stays keyboard accessible — a drop zone alone is not (spec 7, spec 9).
 */
@Directive({
  selector: '[appFileDrop]',
  host: {
    '[class.is-dragging]': 'isDragging()',
  },
})
export class FileDropDirective {
  readonly filesDropped = output<readonly File[]>();

  private readonly dragging = signal(false);
  readonly isDragging = this.dragging.asReadonly();

  @HostListener('dragover', ['$event'])
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragging.set(true);
  }

  @HostListener('dragleave', ['$event'])
  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragging.set(false);
  }

  @HostListener('drop', ['$event'])
  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragging.set(false);

    const files = event.dataTransfer?.files;
    if (files === undefined || files.length === 0) {
      return;
    }
    this.filesDropped.emit(Array.from(files));
  }
}
