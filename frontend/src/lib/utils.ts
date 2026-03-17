/**
 * Utility functions for the frontend application.
 */

/** First letter for avatar fallback - prefers first alphabetic character to avoid "1" for usernames like 1362_xxx */
export function authorInitial(name: string | undefined | null): string {
  if (!name) return '?';
  const match = name.match(/[a-zA-Z\u00C0-\u024F]/);
  return (match?.[0] || name.charAt(0) || '?').toUpperCase();
}
