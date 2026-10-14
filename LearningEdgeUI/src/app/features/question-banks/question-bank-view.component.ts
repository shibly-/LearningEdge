import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin, type Subscription } from 'rxjs';
import { QUESTION_BANK_LIMITS, type QuestionBank } from '../../core/models/question-bank';
import {
  QUESTION_TYPE_OPTIONS,
  questionAnswer,
  questionDetails,
  questionTypeLabel,
  type Question,
  type QuestionContent,
} from '../../core/models/question';
import {
  PROMPT_LIMITS,
  promptStatusBadge,
  promptStatusLabel,
  type PromptRequest,
} from '../../core/models/prompt-request';
import { QuestionBankRepository } from '../../core/services/question-bank.repository';
import { ToastService } from '../../core/services/toast.service';
import { describeError } from '../../core/stores/async-collection.store';
import { BadgeComponent } from '../../shared/components/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { SpinnerComponent } from '../../shared/components/spinner.component';
import { QuestionEditorComponent } from './question-editor.component';

type ViewState = 'loading' | 'loaded' | 'missing' | 'error';
type QuestionMode = 'view' | 'edit';

@Component({
  selector: 'app-question-bank-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    BadgeComponent,
    PageHeaderComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    SpinnerComponent,
    QuestionEditorComponent,
  ],
  templateUrl: './question-bank-view.component.html',
  styleUrl: './question-bank-view.component.scss',
})
export class QuestionBankViewComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly repository = inject(QuestionBankRepository);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(NonNullableFormBuilder);

  private request: Subscription | null = null;

  protected readonly limits = QUESTION_BANK_LIMITS;
  protected readonly promptLimits = PROMPT_LIMITS;
  protected readonly questionTypes = QUESTION_TYPE_OPTIONS;
  protected readonly typeLabel = questionTypeLabel;
  protected readonly details = questionDetails;
  protected readonly answer = questionAnswer;
  protected readonly statusLabel = promptStatusLabel;
  protected readonly statusBadge = promptStatusBadge;

  protected readonly state = signal<ViewState>('loading');
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly bank = signal<QuestionBank | null>(null);
  protected readonly prompts = signal<readonly PromptRequest[]>([]);
  protected readonly questions = signal<readonly Question[]>([]);
  protected readonly savingBank = signal(false);
  protected readonly generating = signal(false);
  protected readonly savingQuestion = signal(false);
  protected readonly selectedId = signal<string | null>(null);
  protected readonly questionMode = signal<QuestionMode | null>(null);

  protected readonly bankForm = this.fb.group({
    title: this.fb.control('', [Validators.required, Validators.maxLength(QUESTION_BANK_LIMITS.title)]),
    isActive: this.fb.control(true),
  });

  protected readonly generateForm = this.fb.group({
    promptText: this.fb.control('', [Validators.required, Validators.maxLength(PROMPT_LIMITS.text)]),
    questionType: this.fb.control(QUESTION_TYPE_OPTIONS[0].id, [Validators.required]),
    questionCount: this.fb.control(5, [
      Validators.required,
      Validators.min(PROMPT_LIMITS.minCount),
      Validators.max(PROMPT_LIMITS.maxCount),
    ]),
  });

  constructor() {
    this.load();
    this.destroyRef.onDestroy(() => this.request?.unsubscribe());
  }

  protected backLink(): readonly string[] {
    return ['/trainings', this.categoryId(), this.trainingId()];
  }

  protected selectedQuestion(): Question | null {
    const id = this.selectedId();
    return this.questions().find((question) => question.id === id) ?? null;
  }

  protected openQuestion(question: Question, mode: QuestionMode): void {
    this.selectedId.set(question.id);
    this.questionMode.set(mode);
  }

  protected closeQuestion(): void {
    this.selectedId.set(null);
    this.questionMode.set(null);
  }

  protected saveBank(): void {
    const current = this.bank();
    if (current === null || this.bankForm.invalid || this.savingBank()) {
      this.bankForm.markAllAsTouched();
      return;
    }

    const value = this.bankForm.getRawValue();
    this.savingBank.set(true);
    this.repository
      .update({
        trainingId: current.trainingId,
        id: current.id,
        title: value.title.trim(),
        isActive: value.isActive,
      })
      .subscribe({
        next: (updated) => {
          this.bank.set(updated);
          this.savingBank.set(false);
          this.toast.success('Question bank saved.');
        },
        error: (error: unknown) => {
          this.savingBank.set(false);
          this.toast.fromFailure(error);
        },
      });
  }

  protected generate(): void {
    const current = this.bank();
    if (current === null || this.generateForm.invalid || this.generating()) {
      this.generateForm.markAllAsTouched();
      return;
    }

    const value = this.generateForm.getRawValue();
    this.generating.set(true);
    this.repository
      .submitPrompt({
        trainingId: current.trainingId,
        questionBankId: current.id,
        promptText: value.promptText.trim(),
        questionType: Number(value.questionType),
        questionCount: Number(value.questionCount),
      })
      .subscribe({
        next: () => {
          this.generating.set(false);
          this.generateForm.reset({
            promptText: '',
            questionType: QUESTION_TYPE_OPTIONS[0].id,
            questionCount: 5,
          });
          this.toast.success('Question generation request submitted.');
          this.reloadPrompts(current.trainingId, current.id);
        },
        error: (error: unknown) => {
          this.generating.set(false);
          this.toast.fromFailure(error);
          this.reloadPrompts(current.trainingId, current.id);
        },
      });
  }

  protected saveQuestion(content: QuestionContent): void {
    const current = this.bank();
    const question = this.selectedQuestion();
    if (current === null || question === null || this.savingQuestion()) {
      return;
    }

    this.savingQuestion.set(true);
    this.repository
      .updateQuestion({
        trainingId: current.trainingId,
        questionBankId: current.id,
        id: question.id,
        ...content,
      })
      .subscribe({
        next: (updated) => {
          this.questions.set(this.questions().map((item) => (item.id === updated.id ? updated : item)));
          this.savingQuestion.set(false);
          this.questionMode.set('view');
          this.toast.success('Question saved.');
        },
        error: (error: unknown) => {
          this.savingQuestion.set(false);
          this.toast.fromFailure(error);
        },
      });
  }

  protected load(): void {
    this.request?.unsubscribe();
    const trainingId = this.trainingId();
    const questionBankId = this.questionBankId();
    if (trainingId.length === 0 || questionBankId.length === 0) {
      this.state.set('missing');
      return;
    }

    this.state.set('loading');
    this.errorMessage.set(null);
    this.request = this.repository.getById(trainingId, questionBankId).subscribe({
      next: (bank) => {
        if (bank === null) {
          this.state.set('missing');
          return;
        }
        this.request = forkJoin({
          prompts: this.repository.listPrompts(trainingId, questionBankId),
          questions: this.repository.listQuestions(trainingId, questionBankId),
        }).subscribe({
          next: ({ prompts, questions }) => {
            this.bank.set(bank);
            this.bankForm.reset({ title: bank.title, isActive: bank.isActive });
            this.prompts.set(prompts);
            this.questions.set(questions);
            this.state.set('loaded');
          },
          error: (error: unknown) => {
            this.errorMessage.set(describeError(error));
            this.state.set('error');
          },
        });
      },
      error: (error: unknown) => {
        this.errorMessage.set(describeError(error));
        this.state.set('error');
      },
    });
  }

  private reloadPrompts(trainingId: string, questionBankId: string): void {
    this.repository.listPrompts(trainingId, questionBankId).subscribe({
      next: (prompts) => this.prompts.set(prompts),
      error: (error: unknown) => this.toast.fromFailure(error),
    });
  }

  private categoryId(): string {
    return this.route.snapshot.paramMap.get('categoryId') ?? '';
  }

  private trainingId(): string {
    return this.route.snapshot.paramMap.get('trainingId') ?? '';
  }

  private questionBankId(): string {
    return this.route.snapshot.paramMap.get('questionBankId') ?? '';
  }
}
