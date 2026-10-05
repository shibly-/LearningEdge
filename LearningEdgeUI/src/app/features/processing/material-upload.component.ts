import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  type OnInit,
  inject,
  viewChild,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ACCEPTED_UPLOAD_EXTENSIONS, stageLabel } from '../../core/models/processing-job';
import type { ProcessingJob } from '../../core/models/processing-job';
import { ProcessingStore } from '../../core/stores/processing.store';
import { BadgeComponent } from '../../shared/components/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { FileDropDirective } from '../../shared/directives/file-drop.directive';

/**
 * Starts the orchestrator pipeline: parse, store paragraphs, generate questions.
 * Progress lives in ProcessingStore so it survives navigation (spec 9).
 */
@Component({
  selector: 'app-material-upload',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DecimalPipe,
    FileDropDirective,
    BadgeComponent,
    PageHeaderComponent,
    EmptyStateComponent,
  ],
  templateUrl: './material-upload.component.html',
  styleUrl: './material-upload.component.scss',
})
export class MaterialUploadComponent implements OnInit {
  protected readonly store = inject(ProcessingStore);

  protected readonly accept = ACCEPTED_UPLOAD_EXTENSIONS.join(',');
  protected readonly stageLabel = stageLabel;

  private readonly fileInput = viewChild.required<ElementRef<HTMLInputElement>>('fileInput');

  ngOnInit(): void {
    this.store.loadRecent();
  }

  protected onDropped(files: readonly File[]): void {
    this.store.upload(files);
  }

  protected onPicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files !== null && input.files.length > 0) {
      this.store.upload(Array.from(input.files));
    }
    // Reset so picking the same file twice still fires a change event.
    input.value = '';
  }

  protected browse(): void {
    this.fileInput().nativeElement.click();
  }

  protected retry(job: ProcessingJob): void {
    this.store.retry(job.id);
  }

  protected dismiss(job: ProcessingJob): void {
    this.store.dismiss(job.id);
  }
}
