import { ApiRequestError } from './httpClient'

export interface ApiErrorPresentation {
  message: string
  traceId?: string
  details: string[]
}

const ERROR_MESSAGES: Record<string, string> = {
  AUTH_INVALID_CREDENTIALS: 'Thông tin đăng nhập không chính xác.',
  ACCOUNT_INACTIVE: 'Tài khoản hiện không hoạt động. Vui lòng liên hệ hỗ trợ.',
  VALIDATION_ERROR: 'Thông tin gửi lên chưa hợp lệ. Vui lòng kiểm tra lại.',
  UNAUTHORIZED: 'Phiên đăng nhập không hợp lệ hoặc đã hết hạn.',
  FORBIDDEN: 'Tài khoản không có quyền thực hiện thao tác này.',
  NOT_FOUND: 'Không tìm thấy dữ liệu được yêu cầu.',
  FACILITY_INACTIVE: 'Địa điểm này hiện không nhận Reservation mới.',
  INVALID_MONTH_RANGE: 'Khoảng tháng thuê không hợp lệ.',
  CAPACITY_NOT_AVAILABLE:
    'Loại kho vừa hết sức chứa trong khoảng tháng đã chọn. Danh sách đã được cập nhật.',
}

export function presentApiError(error: unknown): ApiErrorPresentation {
  if (!(error instanceof ApiRequestError)) {
    return {
      message: 'Không thể kết nối đến máy chủ. Vui lòng thử lại.',
      details: [],
    }
  }

  return {
    message: ERROR_MESSAGES[error.code] ?? 'Yêu cầu không thể hoàn tất. Vui lòng thử lại.',
    traceId: error.traceId,
    details: error.errors ? Object.values(error.errors).flat() : [],
  }
}

export function presentValidationError(message: string): ApiErrorPresentation {
  return { message, details: [] }
}
