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
import { EXAM_LIMITS, type Exam } from '../../core/models/exam';
import { questionAnswer, questionDetails, questionTypeLabel, type Question } from '../../core/models/question';
import type { QuestionBank } from '../../core/models/question-bank';
import { ExamRepository } from '../../core/services/exam.repository';
import { QuestionBankRepository } from '../../core/services/question-bank.repository';
import { ToastService } from '../../core/services/toast.service';
import { describeError } from '../../core/stores/async-collection.store';
import { BadgeComponent } from '../../shared/components/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { SpinnerComponent } from '../../shared/components/spinner.component';

type ViewState = 'loading' | 'loaded' | 'missing' | 'error';
type ExamTab = 'add' | 'attached';

@Component({
  selector: 'app-exam-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    BadgeComponent,
    PageHeaderComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    SpinnerComponent,
  ],
  templateUrl: './exam-view.component.html',
  styleUrl: './exam-view.component.scss',
})
export class ExamViewComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly exams = inject(ExamRepository);
  private readonly banks = inject(QuestionBankRepository);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(NonNullableFormBuilder);

  private request: Subscription | null = null;
  private questionRequest: Subscription | null = null;

  protected readonly limits = EXAM_LIMITS;
  protected readonly typeLabel = questionTypeLabel;
  protected readonly details = questionDetails;
  protected readonly answer = questionAnswer;

  protected readonly state = signal<ViewState>('loading');
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly exam = signal<Exam | null>(null);
  protected readonly questionBanks = signal<readonly QuestionBank[]>([]);
  protected readonly bankQuestions = signal<readonly Question[]>([]);
  protected readonly attached = signal<readonly Question[]>([]);
  protected readonly tab = signal<ExamTab>('add');
  protected readonly selectedBankId = signal('');
  protected readonly saving = signal(false);
  protected readonly addingId = signal<string | null>(null);
  protected readonly removingId = signal<string | null>(null);
  protected readonly loadingQuestions = signal(false);

  protected readonly form = this.fb.group({
    title: this.fb.control('', [Validators.required, Validators.maxLength(EXAM_LIMITS.title)]),
    isActive: this.fb.control(true),
  });

  constructor() {
    this.load();
    this.destroyRef.onDestroy(() => {
      this.request?.unsubscribe();
      this.questionRequest?.unsubscribe();
    });
  }

  protected backLink(): readonly string[] {
    return ['/trainings', this.categoryId(), this.trainingId()];
  }

  protected isAttached(questionId: string): boolean {
    return this.attached().some((question) => question.id === questionId);
  }

  protected selectTab(tab: ExamTab): void {
    this.tab.set(tab);
  }

  protected onBankChange(questionBankId: string): void {
    this.selectedBankId.set(questionBankId);
    this.questionRequest?.unsubscribe();
    if (questionBankId.length === 0) {
      this.bankQuestions.set([]);
      return;
    }

    const bank = this.questionBanks().find((item) => item.id === questionBankId);
    if (bank === undefined) {
      this.bankQuestions.set([]);
      return;
    }

    this.loadingQuestions.set(true);
    this.questionRequest = this.banks.listQuestions(bank.trainingId, bank.id, true).subscribe({
      next: (questions) => {
        this.bankQuestions.set(questions);
        this.loadingQuestions.set(false);
      },
      error: (error: unknown) => {
        this.loadingQuestions.set(false);
        this.toast.fromFailure(error);
      },
    });
  }

  protected saveExam(): void {
    const current = this.exam();
    if (current === null || this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.exams
      .update({
        trainingId: current.trainingId,
        id: current.id,
        title: value.title.trim(),
        isActive: value.isActive,
      })
      .subscribe({
        next: (updated) => {
          this.exam.set(updated);
          this.saving.set(false);
          this.toast.success('Exam saved.');
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.toast.fromFailure(error);
        },
      });
  }

  protected addQuestion(question: Question): void {
    const current = this.exam();
    const questionBankId = this.selectedBankId();
    if (current === null || questionBankId.length === 0 || this.addingId() !== null) {
      return;
    }

    this.addingId.set(question.id);
    this.exams
      .addQuestion({
        trainingId: current.trainingId,
        examId: current.id,
        questionBankId,
        questionId: question.id,
      })
      .subscribe({
        next: () => {
          this.addingId.set(null);
          this.toast.success('Question added to the exam.');
          this.reloadAttached(current.trainingId, current.id);
        },
        error: (error: unknown) => {
          this.addingId.set(null);
          this.toast.fromFailure(error);
        },
      });
  }

  protected removeQuestion(question: Question): void {
    const current = this.exam();
    if (current === null || this.removingId() !== null) {
      return;
    }

    this.removingId.set(question.id);
    this.exams.removeQuestion(current.trainingId, current.id, question.id).subscribe({
      next: () => {
        this.removingId.set(null);
        this.attached.set(this.attached().filter((item) => item.id !== question.id));
        this.toast.success('Question removed from the exam.');
      },
      error: (error: unknown) => {
        this.removingId.set(null);
        this.toast.fromFailure(error);
      },
    });
  }

  protected load(): void {
    this.request?.unsubscribe();
    const trainingId = this.trainingId();
    const examId = this.examId();
    if (trainingId.length === 0 || examId.length === 0) {
      this.state.set('missing');
      return;
    }

    this.state.set('loading');
    this.errorMessage.set(null);
    this.request = this.exams.getById(trainingId, examId).subscribe({
      next: (exam) => {
        if (exam === null) {
          this.state.set('missing');
          return;
        }
        this.request = forkJoin({
          banks: this.banks.listActive(),
          attached: this.exams.listQuestions(trainingId, examId),
        }).subscribe({
          next: ({ banks, attached }) => {
            this.exam.set(exam);
            this.form.reset({ title: exam.title, isActive: exam.isActive });
            this.questionBanks.set(banks);
            this.attached.set(attached);
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

  private reloadAttached(trainingId: string, examId: string): void {
    this.exams.listQuestions(trainingId, examId).subscribe({
      next: (questions) => this.attached.set(questions),
      error: (error: unknown) => this.toast.fromFailure(error),
    });
  }

  private categoryId(): string {
    return this.route.snapshot.paramMap.get('categoryId') ?? '';
  }

  private trainingId(): string {
    return this.route.snapshot.paramMap.get('trainingId') ?? '';
  }

  private examId(): string {
    return this.route.snapshot.paramMap.get('examId') ?? '';
  }
}
