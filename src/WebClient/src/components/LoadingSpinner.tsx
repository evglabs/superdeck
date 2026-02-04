export function LoadingSpinner({ message }: { message?: string }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 12, padding: 40 }}>
      <div className="spinner" />
      {message && <span style={{ color: 'var(--color-text-secondary)' }}>{message}</span>}
    </div>
  )
}
