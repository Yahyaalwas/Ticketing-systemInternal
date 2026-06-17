export function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
}

export function getPriorityColor(priority: string | null | undefined): string {
  switch (priority?.toLowerCase()) {
    case 'critical': return '#FF0000';
    case 'high': return '#FF5630';
    case 'medium': return '#FF8B00';
    case 'low': return '#36B37E';
    default: return '#97A0AF';
  }
}

export function getStatusChipColor(category: string): 'default' | 'primary' | 'success' | 'warning' | 'error' | 'info' {
  switch (category?.toLowerCase()) {
    case 'todo': return 'default';
    case 'in progress': return 'primary';
    case 'done': return 'success';
    case 'blocked': return 'error';
    default: return 'default';
  }
}

export function truncateText(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text;
  return text.slice(0, maxLength) + '...';
}
