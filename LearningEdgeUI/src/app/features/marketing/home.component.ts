import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { AgGridAngular } from 'ag-grid-angular';
import {
  AllCommunityModule,
  ModuleRegistry,
  themeQuartz,
  type ColDef,
} from 'ag-grid-community';
import { HighchartsChartComponent } from 'highcharts-angular';
import type { Options, PointOptionsObject } from 'highcharts';
import type { PlatformSnapshot } from '../../core/models/platform-snapshot';
import { PlatformRepository } from '../../core/services/platform.repository';
import { describeError } from '../../core/stores/async-collection.store';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import {
  PLATFORM_SNAPSHOT,
  SAMPLE_COMPANIES,
  type GrowthRow,
  type QuestionCountRow,
  type SnapshotSlice,
} from './marketing-content';

ModuleRegistry.registerModules([AllCommunityModule]);

const numberFormat = new Intl.NumberFormat('en-US');

function organizationSlices(snapshot: PlatformSnapshot): readonly SnapshotSlice[] {
  return [
    { name: 'Active', y: snapshot.organizationsActive },
    { name: 'Inactive', y: snapshot.organizationsInactive },
  ];
}

function learnerSlices(snapshot: PlatformSnapshot): readonly SnapshotSlice[] {
  return [
    { name: 'Active', y: snapshot.learnersActive },
    { name: 'Pending approval', y: snapshot.learnersPending },
    { name: 'Inactive', y: snapshot.learnersInactive },
  ];
}

function trainingSlices(snapshot: PlatformSnapshot): readonly SnapshotSlice[] {
  return [
    { name: 'Active', y: snapshot.trainingsActive },
    { name: 'Inactive', y: snapshot.trainingsInactive },
  ];
}

function token(name: string): string {
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
}

function pieOptions(description: string, seriesName: string, points: readonly SnapshotSlice[]): Options {
  const colors = [
    token('--le-brand-600'),
    token('--le-success'),
    token('--le-warning'),
    token('--le-info'),
  ].filter((color) => color.length > 0);

  const data: PointOptionsObject[] = points.map((point) => ({ name: point.name, y: point.y }));

  return {
    chart: { type: 'pie', backgroundColor: 'transparent', height: 280 },
    colors,
    title: { text: undefined },
    credits: { enabled: false },
    accessibility: { enabled: true, description },
    tooltip: { pointFormat: '<b>{point.y:,.0f}</b> ({point.percentage:.0f}%)' },
    legend: { align: 'center', verticalAlign: 'bottom' },
    plotOptions: {
      pie: {
        innerSize: '62%',
        showInLegend: true,
        dataLabels: { enabled: false },
      },
    },
    series: [{ type: 'pie', name: seriesName, data }],
  };
}

@Component({
  selector: 'app-home',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, HighchartsChartComponent, AgGridAngular, ErrorStateComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {
  private readonly platform = inject(PlatformRepository);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly companies = SAMPLE_COMPANIES;
  protected readonly questions = PLATFORM_SNAPSHOT.questions;
  protected readonly growth = PLATFORM_SNAPSHOT.growth;

  protected readonly status = signal<'loading' | 'success' | 'error'>('loading');
  protected readonly error = signal<string | null>(null);
  protected readonly organizations = signal<readonly SnapshotSlice[]>([]);
  protected readonly learners = signal<readonly SnapshotSlice[]>([]);
  protected readonly trainings = signal<readonly SnapshotSlice[]>([]);

  protected readonly organizationChart = computed(() =>
    pieOptions(
      'Organizations on the platform, split between active and inactive.',
      'Organizations',
      this.organizations(),
    ),
  );
  protected readonly learnerChart = computed(() =>
    pieOptions(
      'Learner accounts, split between active, pending approval, and inactive.',
      'Learners',
      this.learners(),
    ),
  );
  protected readonly trainingChart = computed(() =>
    pieOptions(
      'Trainings that have been designed, split between active and inactive.',
      'Trainings',
      this.trainings(),
    ),
  );

  constructor() {
    this.reload();
  }

  protected reload(): void {
    this.status.set('loading');
    this.error.set(null);
    this.platform
      .snapshot()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (snapshot) => {
          this.organizations.set(organizationSlices(snapshot));
          this.learners.set(learnerSlices(snapshot));
          this.trainings.set(trainingSlices(snapshot));
          this.status.set('success');
        },
        error: (error: unknown) => {
          this.error.set(describeError(error));
          this.status.set('error');
        },
      });
  }

  protected readonly gridTheme = themeQuartz.withParams({
    accentColor: token('--le-brand-600') || undefined,
    fontFamily: 'Segoe UI, system-ui, sans-serif',
    browserColorScheme: 'light',
    borderRadius: 8,
  });

  protected readonly defaultColDef: ColDef = {
    flex: 1,
    minWidth: 120,
    sortable: true,
    filter: true,
    resizable: true,
  };

  protected readonly questionColumns: ColDef<QuestionCountRow>[] = [
    { field: 'type', headerName: 'Question type' },
    {
      field: 'count',
      headerName: 'Questions created',
      type: 'rightAligned',
      valueFormatter: (params) => numberFormat.format(Number(params.value)),
    },
    {
      headerName: 'Share',
      type: 'rightAligned',
      valueGetter: (params) => {
        const total = this.questions.reduce((sum, row) => sum + row.count, 0);
        const count = params.data?.count ?? 0;
        return total === 0 ? '0%' : `${Math.round((count / total) * 100)}%`;
      },
    },
  ];

  protected readonly growthColumns: ColDef<GrowthRow>[] = [
    { field: 'year', headerName: 'Year', maxWidth: 120 },
    {
      field: 'organizationsAdded',
      headerName: 'Organizations added',
      type: 'rightAligned',
      valueFormatter: (params) => numberFormat.format(Number(params.value)),
    },
    {
      field: 'usersAdded',
      headerName: 'Users added',
      type: 'rightAligned',
      valueFormatter: (params) => numberFormat.format(Number(params.value)),
    },
    { field: 'period', headerName: 'Period' },
  ];

  protected total(slices: readonly SnapshotSlice[]): number {
    return slices.reduce((sum, slice) => sum + slice.y, 0);
  }

  protected countOf(slices: readonly SnapshotSlice[], name: string): number {
    return slices.find((slice) => slice.name === name)?.y ?? 0;
  }

  protected format(value: number): string {
    return numberFormat.format(value);
  }
}
