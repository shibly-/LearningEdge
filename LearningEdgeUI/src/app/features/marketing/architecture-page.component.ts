import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** The architecture lab document, shown inside the public site with a way home. */
@Component({
  selector: 'app-architecture-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  templateUrl: './architecture-page.component.html',
  styleUrl: './architecture-page.component.scss',
})
export class ArchitecturePageComponent {}
