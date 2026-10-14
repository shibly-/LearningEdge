import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { EXAM_LIMITS } from '../../core/models/exam';
import { ExamRepository } from '../../core/services/exam.repository';
import { describeError } from '../../core/stores/async-collection.store';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SpinnerComponent } from '../../shared/components/spinner.component';

@Component({
  selector: 'app-exam-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, SpinnerComponent],
  template: `
    <app-page-header title="Create exam" subtitle="An exam collects questions from active question banks.">
      <a class="le-btn le-btn-secondary" [routerLink]="backLink()">Back to training</a>
    </app-page-header>

    <form class="le-card panel" [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <div class="le-field" [class.is-invalid]="form.controls.title.touched && form.controls.title.invalid">
        <label for="exam-title">Title</label>
        <input id="exam-title" type="text" formControlName="title" [attr.maxlength]="limits.title" />
        @if (form.controls.title.touched && form.controls.title.hasError('required')) {
          <span class="le-error">Title is required.</span>
        }
      </div>

      <label class="check">
        <input type="checkbox" formControlName="isActive" />
        <span>Enabled</span>
      </label>

      @if (errorMessage(); as message) {
        <p class="le-error" role="alert">{{ message }}</p>
      }

      <div class="actions">
        <button type="submit" class="le-btn" [disabled]="saving() || auth.currentUser() === null">
          @if (saving()) {
            <app-spinner [size]="14" label="Creating" />
            <span>Creating…</span>
          } @else {
            <span>Create exam</span>
          }
        </button>
      </div>
    </form>
  `,
  styles: `
    .panel {
      padding: 18px;
    }

    .check {
      display: flex;
      gap: 8px;
      align-items: center;
      margin-bottom: 16px;
    }

    .actions {
      display: flex;
      justify-content: flex-end;
    }
  `,
})
export class ExamFormComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly repository = inject(ExamRepository);
  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly auth = inject(AuthService);

  protected readonly limits = EXAM_LIMITS;
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.group({
    title: this.fb.control('', [Validators.required, Validators.maxLength(EXAM_LIMITS.title)]),
    isActive: this.fb.control(true),
  });

  protected backLink(): readonly string[] {
    return ['/trainings', this.categoryId(), this.trainingId()];
  }

  protected submit(): void {
    const user = this.auth.currentUser();
    if (this.form.invalid || this.saving() || user === null) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    const value = this.form.getRawValue();
    this.repository
      .create({
        trainingId: this.trainingId(),
        preparedByUserId: user.id,
        title: value.title.trim(),
        isActive: value.isActive,
      })
      .subscribe({
        next: (id) => {
          void this.router.navigate(['/trainings', this.categoryId(), this.trainingId(), 'exams', id]);
        },
        error: (error: unknown) => {
          this.errorMessage.set(describeError(error));
          this.saving.set(false);
        },
      });
  }

  private categoryId(): string {
    return this.route.snapshot.paramMap.get('categoryId') ?? '';
  }

  private trainingId(): string {
    return this.route.snapshot.paramMap.get('trainingId') ?? '';
  }
}
