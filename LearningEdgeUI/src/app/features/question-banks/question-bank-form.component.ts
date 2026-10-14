import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { QUESTION_BANK_LIMITS } from '../../core/models/question-bank';
import { QuestionBankRepository } from '../../core/services/question-bank.repository';
import { describeError } from '../../core/stores/async-collection.store';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SpinnerComponent } from '../../shared/components/spinner.component';

@Component({
  selector: 'app-question-bank-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, SpinnerComponent],
  template: `
    <app-page-header title="Create question bank" subtitle="A bank holds generated questions for this training.">
      <a class="le-btn le-btn-secondary" [routerLink]="backLink()">Back to training</a>
    </app-page-header>

    <form class="le-card panel" [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <div class="le-field" [class.is-invalid]="form.controls.title.touched && form.controls.title.invalid">
        <label for="bank-title">Title</label>
        <input id="bank-title" type="text" formControlName="title" [attr.maxlength]="limits.title" />
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
        <button type="submit" class="le-btn" [disabled]="saving()">
          @if (saving()) {
            <app-spinner [size]="14" label="Creating" />
            <span>Creating…</span>
          } @else {
            <span>Create question bank</span>
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
export class QuestionBankFormComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly repository = inject(QuestionBankRepository);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly limits = QUESTION_BANK_LIMITS;
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.group({
    title: this.fb.control('', [Validators.required, Validators.maxLength(QUESTION_BANK_LIMITS.title)]),
    isActive: this.fb.control(true),
  });

  protected backLink(): readonly string[] {
    return ['/trainings', this.categoryId(), this.trainingId()];
  }

  protected submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    const value = this.form.getRawValue();
    this.repository
      .create({
        trainingId: this.trainingId(),
        title: value.title.trim(),
        isActive: value.isActive,
      })
      .subscribe({
        next: (id) => {
          void this.router.navigate([
            '/trainings',
            this.categoryId(),
            this.trainingId(),
            'question-banks',
            id,
          ]);
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
