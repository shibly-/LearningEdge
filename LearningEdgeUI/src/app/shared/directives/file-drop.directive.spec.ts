import { TestBed } from '@angular/core/testing';
import { FileDropDirective } from './file-drop.directive';

function dragEvent(): DragEvent {
  return { preventDefault: () => undefined, stopPropagation: () => undefined } as DragEvent;
}

describe('FileDropDirective', () => {
  let directive: FileDropDirective;

  beforeEach(() => {
    directive = TestBed.runInInjectionContext(() => new FileDropDirective());
  });

  it('stays in the dragging state while crossing child elements', () => {
    directive.onDragEnter(dragEvent()); // zone
    directive.onDragEnter(dragEvent()); // child
    directive.onDragLeave(dragEvent()); // left the child, still inside the zone

    expect(directive.isDragging()).toBe(true);

    directive.onDragLeave(dragEvent()); // left the zone
    expect(directive.isDragging()).toBe(false);
  });

  it('clears the state on drop', () => {
    directive.onDragEnter(dragEvent());
    directive.onDragEnter(dragEvent());
    directive.onDrop({ ...dragEvent(), dataTransfer: null } as DragEvent);

    expect(directive.isDragging()).toBe(false);
  });
});
