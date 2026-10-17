const vndFormatter = new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'VND',
  maximumFractionDigits: 2,
})

export function formatEstimatedCost(amount: number | null) {
  return amount === null ? 'Not provided' : vndFormatter.format(amount)
}
