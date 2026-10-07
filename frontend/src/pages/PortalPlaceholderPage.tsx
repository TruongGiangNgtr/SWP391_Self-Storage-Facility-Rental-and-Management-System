export function PortalPlaceholderPage({ title }: { title: string }) {
  return (
    <main className="page-container">
      <section className="panel">
        <h1>{title}</h1>
        <p>This portal is ready for the features defined in SRS V10.</p>
      </section>
    </main>
  )
}
