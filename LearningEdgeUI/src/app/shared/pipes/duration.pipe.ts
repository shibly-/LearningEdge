import { Pipe } from '@angular/core';
import type { PipeTransform } from '@angular/core';

/** Minutes to a compact human duration: 45 -> "45m", 90 -> "1h 30m". */
@Pipe({ name: 'duration' })
export class DurationPipe implements PipeTransform {
  transform(minutes: number | null | undefined): string {
    if (minutes === null || minutes === undefined || minutes <= 0) {
      return '—';
    }
    const hours = Math.floor(minutes / 60);
    const remainder = minutes % 60;

    if (hours === 0) {
      return `${remainder}m`;
    }
    return remainder === 0 ? `${hours}h` : `${hours}h ${remainder}m`;
  }
}
