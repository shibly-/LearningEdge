import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { ApiService } from '../http/api.service';
import { ApiFailure } from '../http/api-result';
import { apiPaths } from '../http/api-paths';
import type {
  CreateQuestionBankCommand,
  QuestionBank,
  UpdateQuestionBankCommand,
} from '../models/question-bank';
import { QUESTION_BANK_LIMITS } from '../models/question-bank';
import {
  QUESTION_TYPE_OPTIONS,
  type Question,
  type UpdateQuestionCommand,
} from '../models/question';
import {
  PromptStatus,
  type PromptRequest,
  type SubmitPromptRequestCommand,
} from '../models/prompt-request';
import { PROMPT_LIMITS } from '../models/prompt-request';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS } from './repository-support';

export abstract class QuestionBankRepository {
  abstract listByTraining(trainingId: string): Observable<readonly QuestionBank[]>;
  abstract listActive(): Observable<readonly QuestionBank[]>;
  abstract getById(trainingId: string, id: string): Observable<QuestionBank | null>;
  abstract create(command: CreateQuestionBankCommand): Observable<string>;
  abstract update(command: UpdateQuestionBankCommand): Observable<QuestionBank>;
  abstract listPrompts(trainingId: string, questionBankId: string): Observable<readonly PromptRequest[]>;
  abstract submitPrompt(command: SubmitPromptRequestCommand): Observable<string>;
  abstract listQuestions(
    trainingId: string,
    questionBankId: string,
    activeOnly?: boolean,
  ): Observable<readonly Question[]>;
  abstract updateQuestion(command: UpdateQuestionCommand): Observable<Question>;
}

@Injectable()
export class HttpQuestionBankRepository extends QuestionBankRepository {
  private readonly api = inject(ApiService);

  override listByTraining(trainingId: string): Observable<readonly QuestionBank[]> {
    return this.api.get(apiPaths.questionBank.list(trainingId));
  }

  override listActive(): Observable<readonly QuestionBank[]> {
    return this.api.get(apiPaths.questionBank.active());
  }

  override getById(trainingId: string, id: string): Observable<QuestionBank | null> {
    return this.api.getOptional(apiPaths.questionBank.byId(trainingId, id));
  }

  override create(command: CreateQuestionBankCommand): Observable<string> {
    const { trainingId, ...body } = command;
    return this.api.post(apiPaths.questionBank.list(trainingId), body);
  }

  override update(command: UpdateQuestionBankCommand): Observable<QuestionBank> {
    const { trainingId, id, ...body } = command;
    return this.api.put(apiPaths.questionBank.byId(trainingId, id), body);
  }

  override listPrompts(trainingId: string, questionBankId: string): Observable<readonly PromptRequest[]> {
    return this.api.get(apiPaths.questionBank.prompts(trainingId, questionBankId));
  }

  override submitPrompt(command: SubmitPromptRequestCommand): Observable<string> {
    const { trainingId, questionBankId, ...body } = command;
    return this.api.post(apiPaths.questionBank.prompts(trainingId, questionBankId), body);
  }

  override listQuestions(
    trainingId: string,
    questionBankId: string,
    activeOnly = false,
  ): Observable<readonly Question[]> {
    return this.api.get(apiPaths.questionBank.questions(trainingId, questionBankId, activeOnly));
  }

  override updateQuestion(command: UpdateQuestionCommand): Observable<Question> {
    const { trainingId, questionBankId, id, ...body } = command;
    return this.api.put(apiPaths.questionBank.question(trainingId, questionBankId, id), body);
  }
}

@Injectable()
export class MockQuestionBankRepository extends QuestionBankRepository {
  private readonly db = inject(MockDb);
  private banks: QuestionBank[] = [];
  private prompts: PromptRequest[] = [];
  private questions: Question[] = [];

  override listByTraining(trainingId: string): Observable<readonly QuestionBank[]> {
    return of(this.banks.filter((bank) => bank.trainingId === trainingId)).pipe(delay(MOCK_LATENCY_MS));
  }

  override listActive(): Observable<readonly QuestionBank[]> {
    return of(this.banks.filter((bank) => bank.isActive)).pipe(delay(MOCK_LATENCY_MS));
  }

  override getById(trainingId: string, id: string): Observable<QuestionBank | null> {
    return of(this.banks.find((bank) => bank.id === id && bank.trainingId === trainingId) ?? null).pipe(
      delay(MOCK_LATENCY_MS),
    );
  }

  override create(command: CreateQuestionBankCommand): Observable<string> {
    const title = command.title.trim();
    if (title.length === 0 || title.length > QUESTION_BANK_LIMITS.title) {
      return throwError(() => new ApiFailure('validation', 'Question bank title is required.', 400));
    }
    if (!this.db.trainings.some((training) => training.id === command.trainingId)) {
      return throwError(
        () => new ApiFailure('not-found', `No training found with Id ${command.trainingId}.`, 404),
      );
    }
    if (this.banks.some((bank) => bank.trainingId === command.trainingId && bank.title === title)) {
      return throwError(
        () => new ApiFailure('conflict', `A question bank named ${title} already exists for this training.`, 409),
      );
    }

    const id = this.db.nextGuid();
    this.banks = [
      ...this.banks,
      { id, trainingId: command.trainingId, title, isActive: command.isActive, createdAt: new Date().toISOString() },
    ];
    return of(id).pipe(delay(MOCK_LATENCY_MS));
  }

  override update(command: UpdateQuestionBankCommand): Observable<QuestionBank> {
    const existing = this.banks.find((bank) => bank.id === command.id && bank.trainingId === command.trainingId);
    if (existing === undefined) {
      return throwError(
        () => new ApiFailure('not-found', `No question bank found with Id ${command.id}.`, 404),
      );
    }
    const title = command.title.trim();
    const updated: QuestionBank = { ...existing, title, isActive: command.isActive };
    this.banks = this.banks.map((bank) => (bank.id === updated.id ? updated : bank));
    return of(updated).pipe(delay(MOCK_LATENCY_MS));
  }

  override listPrompts(trainingId: string, questionBankId: string): Observable<readonly PromptRequest[]> {
    const items = this.prompts
      .filter((prompt) => prompt.trainingId === trainingId && prompt.questionBankId === questionBankId)
      .sort((left, right) => right.requestedOn.localeCompare(left.requestedOn));
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override submitPrompt(command: SubmitPromptRequestCommand): Observable<string> {
    const promptText = command.promptText.trim();
    if (promptText.length === 0 || promptText.length > PROMPT_LIMITS.text) {
      return throwError(() => new ApiFailure('validation', 'Prompt text is required.', 400));
    }
    if (!QUESTION_TYPE_OPTIONS.some((option) => option.id === command.questionType)) {
      return throwError(() => new ApiFailure('validation', 'Question type is not supported.', 400));
    }
    if (command.questionCount < PROMPT_LIMITS.minCount || command.questionCount > PROMPT_LIMITS.maxCount) {
      return throwError(() => new ApiFailure('validation', 'Question count must be between 1 and 50.', 400));
    }

    const id = this.db.nextGuid();
    this.prompts = [
      {
        id,
        trainingId: command.trainingId,
        questionBankId: command.questionBankId,
        promptText,
        questionType: command.questionType,
        questionCount: command.questionCount,
        requestedOn: new Date().toISOString(),
        status: PromptStatus.Pending,
      },
      ...this.prompts,
    ];
    return of(id).pipe(delay(MOCK_LATENCY_MS));
  }

  override listQuestions(
    trainingId: string,
    questionBankId: string,
    activeOnly = false,
  ): Observable<readonly Question[]> {
    if (!this.banks.some((bank) => bank.id === questionBankId && bank.trainingId === trainingId)) {
      return throwError(
        () => new ApiFailure('not-found', `No question bank found with Id ${questionBankId}.`, 404),
      );
    }
    const items = this.questions.filter(
      (question) => question.questionBankId === questionBankId && (!activeOnly || question.isActive),
    );
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override updateQuestion(command: UpdateQuestionCommand): Observable<Question> {
    const existing = this.questions.find(
      (question) => question.id === command.id && question.questionBankId === command.questionBankId,
    );
    if (existing === undefined) {
      return throwError(() => new ApiFailure('not-found', `No question found with Id ${command.id}.`, 404));
    }
    const updated: Question = {
      ...existing,
      text: command.text.trim(),
      score: command.score,
      isActive: command.isActive,
      options: command.options,
      correctOptions: command.correctOptions,
      correctValue: command.correctValue,
    };
    this.questions = this.questions.map((question) => (question.id === updated.id ? updated : question));
    return of(updated).pipe(delay(MOCK_LATENCY_MS));
  }
}
