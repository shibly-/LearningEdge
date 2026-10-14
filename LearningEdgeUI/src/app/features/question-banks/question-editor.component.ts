import { ChangeDetectionStrategy, Component, OnInit, inject, input, output, signal } from '@angular/core';
import {
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
  type FormControl,
  type FormGroup,
} from '@angular/forms';
import {
  QUESTION_LIMITS,
  QuestionType,
  type Question,
  type QuestionContent,
} from '../../core/models/question';
import { SpinnerComponent } from '../../shared/components/spinner.component';

interface OptionControl {
  text: FormControl<string>;
  correct: FormControl<boolean>;
}

@Component({
  selector: 'app-question-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SpinnerComponent],
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <div class="le-field" [class.is-invalid]="form.controls.text.touched && form.controls.text.invalid">
        <label for="question-text">Question</label>
        <textarea id="question-text" formControlName="text" [attr.maxlength]="limits.text"></textarea>
        @if (form.controls.text.touched && form.controls.text.hasError('required')) {
          <span class="le-error">Question text is required.</span>
        }
      </div>

      <div class="le-field" [class.is-invalid]="form.controls.score.touched && form.controls.score.invalid">
        <label for="question-score">Score</label>
        <input id="question-score" type="number" formControlName="score" [min]="limits.minScore" [max]="limits.maxScore" />
        @if (form.controls.score.touched && form.controls.score.invalid) {
          <span class="le-error">Score must be between {{ limits.minScore }} and {{ limits.maxScore }}.</span>
        }
      </div>

      <label class="check">
        <input type="checkbox" formControlName="isActive" />
        <span>Enabled</span>
      </label>

      @if (question().type === trueFalse) {
        <fieldset class="choices">
          <legend>Correct answer</legend>
          <label class="check">
            <input
              type="radio"
              name="correct-value"
              [checked]="form.controls.correctValue.value === true"
              (change)="form.controls.correctValue.setValue(true)"
            />
            <span>True</span>
          </label>
          <label class="check">
            <input
              type="radio"
              name="correct-value"
              [checked]="form.controls.correctValue.value === false"
              (change)="form.controls.correctValue.setValue(false)"
            />
            <span>False</span>
          </label>
        </fieldset>
      } @else {
        <div class="options" formArrayName="options">
          <div class="options-head">
            <h3>Options</h3>
            <button type="button" class="le-btn le-btn-secondary le-btn-sm" (click)="addOption()" [disabled]="options.length >= limits.maxOptions">
              Add option
            </button>
          </div>
          @for (group of options.controls; track $index) {
            <div class="option" [formGroupName]="$index">
              <label class="check">
                <input
                  [type]="question().type === multiple ? 'checkbox' : 'radio'"
                  formControlName="correct"
                  [name]="'correct-option'"
                  (change)="markCorrect($index)"
                />
                <span class="le-visually-hidden">Correct</span>
              </label>
              <input type="text" formControlName="text" [attr.maxlength]="limits.option" [attr.aria-label]="'Option ' + ($index + 1)" />
              <button type="button" class="le-btn le-btn-secondary le-btn-sm" (click)="removeOption($index)" [disabled]="options.length <= limits.minOptions">
                Remove
              </button>
            </div>
          }
          <p class="le-hint">{{ question().type === multiple ? 'Tick every correct option.' : 'Choose the one correct option.' }}</p>
        </div>
      }

      @if (formError(); as message) {
        <p class="le-error" role="alert">{{ message }}</p>
      }

      <div class="actions">
        <button type="button" class="le-btn le-btn-secondary" (click)="cancel.emit()">Cancel</button>
        <button type="submit" class="le-btn" [disabled]="saving()">
          @if (saving()) {
            <app-spinner [size]="14" label="Saving" />
            <span>Saving…</span>
          } @else {
            <span>Save question</span>
          }
        </button>
      </div>
    </form>
  `,
  styles: `
    .check {
      display: flex;
      gap: 8px;
      align-items: center;
      margin-bottom: 12px;
    }

    .choices {
      border: 0;
      margin: 0 0 12px;
      padding: 0;
    }

    legend,
    h3 {
      font-size: 0.95rem;
      margin: 0 0 8px;
    }

    .options-head,
    .option,
    .actions {
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .options-head,
    .actions {
      justify-content: space-between;
      margin-bottom: 12px;
    }

    .actions {
      justify-content: flex-end;
    }

    .option {
      margin-bottom: 8px;
    }

    .option input[type='text'] {
      flex: 1;
    }

    .le-hint,
    .le-error {
      margin: 0 0 12px;
    }
  `,
})
export class QuestionEditorComponent implements OnInit {
  readonly question = input.required<Question>();
  readonly saving = input(false);
  readonly save = output<QuestionContent>();
  readonly cancel = output<void>();

  protected readonly limits = QUESTION_LIMITS;
  protected readonly trueFalse = QuestionType.TrueFalse;
  protected readonly multiple = QuestionType.MultipleSelection;
  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly formError = signal<string | null>(null);

  protected readonly form = this.fb.group({
    text: this.fb.control('', [Validators.required, Validators.maxLength(QUESTION_LIMITS.text)]),
    score: this.fb.control(1, [
      Validators.required,
      Validators.min(QUESTION_LIMITS.minScore),
      Validators.max(QUESTION_LIMITS.maxScore),
    ]),
    isActive: this.fb.control(true),
    correctValue: this.fb.control(true),
    options: this.fb.array<FormGroup<OptionControl>>([]),
  });

  ngOnInit(): void {
    const question = this.question();
    this.form.patchValue({
      text: question.text,
      score: question.score,
      isActive: question.isActive,
      correctValue: question.correctValue ?? true,
    });
    for (const option of question.options) {
      const correct = question.correctOptions.some(
        (answer) => answer.toLowerCase() === option.toLowerCase(),
      );
      this.options.push(this.optionGroup(option, correct));
    }
    while (question.type !== QuestionType.TrueFalse && this.options.length < QUESTION_LIMITS.minOptions) {
      this.options.push(this.optionGroup('', false));
    }
  }

  protected get options() {
    return this.form.controls.options;
  }

  protected addOption(): void {
    if (this.options.length >= QUESTION_LIMITS.maxOptions) {
      return;
    }
    this.options.push(this.optionGroup('', false));
  }

  protected removeOption(index: number): void {
    if (this.options.length <= QUESTION_LIMITS.minOptions) {
      return;
    }
    this.options.removeAt(index);
  }

  protected markCorrect(index: number): void {
    if (this.question().type === QuestionType.MultipleSelection) {
      return;
    }
    this.options.controls.forEach((group, optionIndex) => {
      group.controls.correct.setValue(optionIndex === index);
    });
  }

  protected submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const question = this.question();
    if (question.type === QuestionType.TrueFalse) {
      this.save.emit({
        text: value.text.trim(),
        score: Number(value.score),
        isActive: value.isActive,
        options: [],
        correctOptions: [],
        correctValue: value.correctValue,
      });
      return;
    }

    const options = value.options.map((option) => option.text.trim()).filter((text) => text.length > 0);
    const correctOptions = value.options
      .filter((option) => option.correct)
      .map((option) => option.text.trim())
      .filter((text) => text.length > 0);

    if (options.length < QUESTION_LIMITS.minOptions) {
      this.formError.set(`Provide at least ${QUESTION_LIMITS.minOptions} options.`);
      return;
    }
    if (question.type === QuestionType.Mcq && correctOptions.length !== 1) {
      this.formError.set('Choose exactly one correct option.');
      return;
    }
    if (question.type === QuestionType.MultipleSelection && correctOptions.length === 0) {
      this.formError.set('Tick at least one correct option.');
      return;
    }
    this.formError.set(null);

    this.save.emit({
      text: value.text.trim(),
      score: Number(value.score),
      isActive: value.isActive,
      options,
      correctOptions,
      correctValue: null,
    });
  }

  private optionGroup(text: string, correct: boolean): FormGroup<OptionControl> {
    return this.fb.group({
      text: this.fb.control(text, [Validators.maxLength(QUESTION_LIMITS.option)]),
      correct: this.fb.control(correct),
    });
  }
}
