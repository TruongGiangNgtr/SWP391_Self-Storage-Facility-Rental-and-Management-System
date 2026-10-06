import { Link } from 'react-router-dom'

export function CustomerPortalPage() {
  return (
    <main className="page-container">
      <section className="panel">
        <p className="eyebrow">Customer Portal</p>
        <h1>Customer Portal</h1>
        <h2>Quản lý dịch vụ lưu trữ</h2>
        <p className="muted">
          Tạo yêu cầu đặt kho theo tháng. Các chức năng thanh toán Deposit và lịch
          bàn giao sẽ được bổ sung trong các feature tiếp theo.
        </p>
        <div className="hero-actions">
          <Link className="button" to="/customer/reservations/new">
            Tạo Reservation
          </Link>
        </div>
      </section>
    </main>
  )
}
