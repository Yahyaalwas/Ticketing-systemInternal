import { formatDistanceToNow as fnsFormatDistanceToNow, format, parseISO } from 'date-fns';

export function formatDistanceToNow(dateStr: string | Date): string {
  const date = typeof dateStr === 'string' ? parseISO(dateStr) : dateStr;
  return fnsFormatDistanceToNow(date, { addSuffix: true });
}

export function formatDate(dateStr: string | Date | null | undefined): string {
  if (!dateStr) return '—';
  const date = typeof dateStr === 'string' ? parseISO(dateStr) : dateStr;
  return format(date, 'MMM d, yyyy');
}

export function formatDateTime(dateStr: string | Date | null | undefined): string {
  if (!dateStr) return '—';
  const date = typeof dateStr === 'string' ? parseISO(dateStr) : dateStr;
  return format(date, 'MMM d, yyyy HH:mm');
}
