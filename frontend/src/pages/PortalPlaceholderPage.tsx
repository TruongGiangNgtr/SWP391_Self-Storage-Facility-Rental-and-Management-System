export function PortalPlaceholderPage({ title }: { title: string }) {
  return (
    <main className="page-container">
      <section className="panel">
        <h1>{title}</h1>
        <p>Nền tảng route và phân quyền đã sẵn sàng. Feature cụ thể sẽ được thêm theo SRS.</p>
      </section>
    </main>
  )
}
