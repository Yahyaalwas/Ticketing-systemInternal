import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';

dayjs.extend(relativeTime);

export function formatDistanceToNow(dateStr: string | Date): string {
  return dayjs(dateStr).fromNow();
}

export function formatDate(dateStr: string | Date | null | undefined): string {
  if (!dateStr) return '—';
  return dayjs(dateStr).format('MMM D, YYYY');
}

export function formatDateTime(dateStr: string | Date | null | undefined): string {
  if (!dateStr) return '—';
  return dayjs(dateStr).format('MMM D, YYYY HH:mm');
}
