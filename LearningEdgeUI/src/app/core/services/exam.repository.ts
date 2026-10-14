import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { ApiService } from '../http/api.service';
import { ApiFailure } from '../http/api-result';
import { apiPaths } from '../http/api-paths';
import type { AddExamQuestionCommand, CreateExamCommand, Exam, UpdateExamCommand } from '../models/exam';
import { EXAM_LIMITS } from '../models/exam';
import type { Question } from '../models/question';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS } from './repository-support';

export abstract class ExamRepository {
  abstract listByTraining(trainingId: string): Observable<readonly Exam[]>;
  abstract getById(trainingId: string, id: string): Observable<Exam | null>;
  abstract create(command: CreateExamCommand): Observable<string>;
  abstract update(command: UpdateExamCommand): Observable<Exam>;
  abstract listQuestions(trainingId: string, examId: string): Observable<readonly Question[]>;
  abstract addQuestion(command: AddExamQuestionCommand): Observable<string>;
  abstract removeQuestion(trainingId: string, examId: string, questionId: string): Observable<void>;
}

@Injectable()
export class HttpExamRepository extends ExamRepository {
  private readonly api = inject(ApiService);

  override listByTraining(trainingId: string): Observable<readonly Exam[]> {
    return this.api.get(apiPaths.exam.list(trainingId));
  }

  override getById(trainingId: string, id: string): Observable<Exam | null> {
    return this.api.getOptional(apiPaths.exam.byId(trainingId, id));
  }

  override create(command: CreateExamCommand): Observable<string> {
    const { trainingId, ...body } = command;
    return this.api.post(apiPaths.exam.list(trainingId), body);
  }

  override update(command: UpdateExamCommand): Observable<Exam> {
    const { trainingId, id, ...body } = command;
    return this.api.put(apiPaths.exam.byId(trainingId, id), body);
  }

  override listQuestions(trainingId: string, examId: string): Observable<readonly Question[]> {
    return this.api.get(apiPaths.exam.questions(trainingId, examId));
  }

  override addQuestion(command: AddExamQuestionCommand): Observable<string> {
    const { trainingId, examId, ...body } = command;
    return this.api.post(apiPaths.exam.questions(trainingId, examId), body);
  }

  override removeQuestion(trainingId: string, examId: string, questionId: string): Observable<void> {
    return this.api.delete(apiPaths.exam.question(trainingId, examId, questionId));
  }
}

@Injectable()
export class MockExamRepository extends ExamRepository {
  private readonly db = inject(MockDb);
  private exams: Exam[] = [];
  private links: { examId: string; question: Question }[] = [];

  override listByTraining(trainingId: string): Observable<readonly Exam[]> {
    return of(this.exams.filter((exam) => exam.trainingId === trainingId)).pipe(delay(MOCK_LATENCY_MS));
  }

  override getById(trainingId: string, id: string): Observable<Exam | null> {
    return of(this.exams.find((exam) => exam.id === id && exam.trainingId === trainingId) ?? null).pipe(
      delay(MOCK_LATENCY_MS),
    );
  }

  override create(command: CreateExamCommand): Observable<string> {
    const title = command.title.trim();
    if (title.length === 0 || title.length > EXAM_LIMITS.title) {
      return throwError(() => new ApiFailure('validation', 'Exam title is required.', 400));
    }
    if (this.exams.some((exam) => exam.trainingId === command.trainingId && exam.title === title)) {
      return throwError(
        () => new ApiFailure('conflict', `An exam named ${title} already exists for this training.`, 409),
      );
    }
    const id = this.db.nextGuid();
    this.exams = [
      ...this.exams,
      {
        id,
        trainingId: command.trainingId,
        preparedByUserId: command.preparedByUserId,
        title,
        isActive: command.isActive,
        createdAt: new Date().toISOString(),
      },
    ];
    return of(id).pipe(delay(MOCK_LATENCY_MS));
  }

  override update(command: UpdateExamCommand): Observable<Exam> {
    const existing = this.exams.find((exam) => exam.id === command.id && exam.trainingId === command.trainingId);
    if (existing === undefined) {
      return throwError(() => new ApiFailure('not-found', `No exam found with Id ${command.id}.`, 404));
    }
    const updated: Exam = { ...existing, title: command.title.trim(), isActive: command.isActive };
    this.exams = this.exams.map((exam) => (exam.id === updated.id ? updated : exam));
    return of(updated).pipe(delay(MOCK_LATENCY_MS));
  }

  override listQuestions(trainingId: string, examId: string): Observable<readonly Question[]> {
    if (!this.exams.some((exam) => exam.id === examId && exam.trainingId === trainingId)) {
      return throwError(() => new ApiFailure('not-found', `No exam found with Id ${examId}.`, 404));
    }
    return of(this.links.filter((link) => link.examId === examId).map((link) => link.question)).pipe(
      delay(MOCK_LATENCY_MS),
    );
  }

  override addQuestion(command: AddExamQuestionCommand): Observable<string> {
    if (this.links.some((link) => link.examId === command.examId && link.question.id === command.questionId)) {
      return throwError(() => new ApiFailure('conflict', 'That question is already on this exam.', 409));
    }
    return throwError(
      () => new ApiFailure('validation', 'Only an active question from the selected question bank can be added.', 400),
    );
  }

  override removeQuestion(trainingId: string, examId: string, questionId: string): Observable<void> {
    if (!this.exams.some((exam) => exam.id === examId && exam.trainingId === trainingId)) {
      return throwError(() => new ApiFailure('not-found', `No exam found with Id ${examId}.`, 404));
    }
    this.links = this.links.filter((link) => !(link.examId === examId && link.question.id === questionId));
    return of(undefined).pipe(delay(MOCK_LATENCY_MS));
  }
}
