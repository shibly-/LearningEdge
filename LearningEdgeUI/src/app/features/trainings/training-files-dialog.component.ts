import { DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import {
  TRAINING_FILE_RULES,
  validateTrainingFiles,
  type Training,
  type TrainingFile,
} from '../../core/models/training';
import { UserRole } from '../../core/models/user-role';
import { TrainingStore } from '../../core/stores/training.store';
import { ModalShellComponent } from '../../shared/components/modal-shell.component';
import { SpinnerComponent } from '../../shared/components/spinner.component';
import { FileDropDirective } from '../../shared/directives/file-drop.directive';

const UPLOAD_ROLES: readonly UserRole[] = [UserRole.OrgAdmin, UserRole.SysAdmin];

@Component({
  selector: 'app-training-files-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe, ModalShellComponent, SpinnerComponent, FileDropDirective],
  template: `
    <app-modal-shell [title]="'Files for ' + training().name" (close)="close.emit()">
      @if (current().files.length === 0) {
        <p class="muted">No files uploaded yet.</p>
      } @else {
        <ul class="files" aria-label="Uploaded files">
          @for (file of current().files; track file.id) {
            <li>
              <span class="name">{{ file.fileName }}</span>
              <span class="file-meta">
                <span class="muted">
                  {{ file.sizeBytes / 1024 | number: '1.0-0' }} KB ·
                  {{ file.createdAt | date: 'mediumDate' }}
                </span>
                @if (canUpload()) {
                  <button
                    type="button"
                    class="le-btn le-btn-danger le-btn-sm"
                    [attr.aria-label]="'Remove ' + file.fileName"
                    [disabled]="store.removingFileId() !== null || store.isUploading()"
                    (click)="remove(file)"
                  >
                    @if (store.removingFileId() === file.id) {
                      <app-spinner [size]="14" label="Removing" />
                    } @else {
                      <span>Remove</span>
                    }
                  </button>
                }
              </span>
            </li>
          }
        </ul>
      }

      @if (canUpload()) {
        <section
          class="dropzone"
          appFileDrop
          (filesDropped)="select($event)"
          aria-labelledby="training-files-heading"
        >
          <h3 id="training-files-heading">Upload files</h3>
          <p class="muted">
            PDF, DOCX or TXT · up to {{ rules.maxFiles }} files, 50 MB each, 100 MB in total.
          </p>
          <button type="button" class="le-btn le-btn-secondary" (click)="browse()">
            Choose files
          </button>
          <label class="le-visually-hidden" for="training-files-input"
            >Choose files to upload</label
          >
          <input
            #fileInput
            id="training-files-input"
            class="le-visually-hidden"
            type="file"
            multiple
            [accept]="accept"
            (change)="onPicked($event)"
          />
        </section>

        @if (selected().length > 0) {
          <ul class="files" aria-label="Selected files">
            @for (file of selected(); track $index) {
              <li>
                <span class="name">{{ file.name }}</span>
                <span class="file-meta">
                  <span class="muted">{{ file.size / 1024 | number: '1.0-0' }} KB</span>
                  <button
                    type="button"
                    class="le-btn le-btn-secondary le-btn-sm"
                    [attr.aria-label]="'Remove ' + file.name + ' from selection'"
                    (click)="unselect($index)"
                  >
                    Remove
                  </button>
                </span>
              </li>
            }
          </ul>
        }

        @if (problem() !== null) {
          <p class="le-error" role="alert">{{ problem() }}</p>
        }

        <div class="actions">
          <button type="button" class="le-btn le-btn-secondary" (click)="close.emit()">
            Close
          </button>
          <button
            type="button"
            class="le-btn"
            [disabled]="
              store.isUploading() || store.removingFileId() !== null || selected().length === 0
            "
            (click)="upload()"
          >
            @if (store.isUploading()) {
              <app-spinner [size]="14" label="Uploading" />
              <span>Uploading…</span>
            } @else {
              <span>Upload {{ selected().length || '' }} file(s)</span>
            }
          </button>
        </div>
      } @else {
        <p class="muted">Only organization admins can upload files.</p>
      }
    </app-modal-shell>
  `,
  styles: `
    .files {
      list-style: none;
      margin: 0 0 16px;
      padding: 0;
    }

    .files li {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      padding: 6px 0;
      border-bottom: 1px solid var(--le-border);
    }

    .file-meta {
      display: flex;
      flex-shrink: 0;
      align-items: center;
      gap: 10px;
    }

    .name {
      overflow-wrap: anywhere;
    }

    .muted {
      margin: 0;
      color: var(--le-text-muted);
      font-size: 0.85rem;
    }

    .dropzone {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: 8px;
      margin-bottom: 14px;
      padding: 16px;
      border: 2px dashed var(--le-border);
      border-radius: var(--le-radius);
    }

    .dropzone.is-dragging {
      border-color: var(--le-brand-500);
      background: var(--le-brand-50);
    }

    h3 {
      margin: 0;
      font-size: 1rem;
    }

    .actions {
      display: flex;
      gap: 8px;
      justify-content: flex-end;
      margin-top: 14px;
    }
  `,
})
export class TrainingFilesDialogComponent {
  readonly training = input.required<Training>();
  readonly close = output<void>();

  protected readonly store = inject(TrainingStore);
  private readonly auth = inject(AuthService);

  protected readonly rules = TRAINING_FILE_RULES;
  protected readonly accept = TRAINING_FILE_RULES.extensions.join(',');

  private readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');

  protected readonly selected = signal<readonly File[]>([]);
  protected readonly problem = signal<string | null>(null);

  /** The store copy, so freshly uploaded files show without reopening. */
  protected readonly current = computed(
    () => this.store.findById(this.training().id) ?? this.training(),
  );
  protected readonly canUpload = computed(() => this.auth.hasAnyRole(UPLOAD_ROLES));

  protected browse(): void {
    this.fileInput()?.nativeElement.click();
  }

  protected onPicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.select(Array.from(input.files ?? []));
    input.value = '';
  }

  protected select(files: readonly File[]): void {
    this.selected.set(files);
    this.problem.set(validateTrainingFiles(files));
  }

  protected unselect(index: number): void {
    const next = this.selected().filter((_, i) => i !== index);
    this.selected.set(next);
    this.problem.set(next.length === 0 ? null : validateTrainingFiles(next));
  }

  protected remove(file: TrainingFile): void {
    const user = this.auth.currentUser();
    if (user === null) {
      this.problem.set('Sign in again to remove files.');
      return;
    }

    const training = this.current();
    this.store.removeFile(
      {
        organizationId: training.organizationId,
        categoryId: training.categoryId,
        trainingId: training.id,
        fileId: file.id,
        removedByUserId: user.id,
      },
      () => this.problem.set(null),
    );
  }

  protected upload(): void {
    const files = this.selected();
    const problem = validateTrainingFiles(files);
    const user = this.auth.currentUser();
    if (problem !== null || user === null) {
      this.problem.set(problem ?? 'Sign in again to upload files.');
      return;
    }

    const training = this.training();
    this.store.uploadFiles(
      {
        organizationId: training.organizationId,
        categoryId: training.categoryId,
        trainingId: training.id,
        uploadedByUserId: user.id,
        files,
      },
      () => {
        this.selected.set([]);
        this.problem.set(null);
      },
    );
  }
}
