export interface CategoryDto {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  isActive: boolean;
  displayOrder: number;
}

export interface ProductListItemDto {
  id: string;
  name: string;
  slug: string;
  price: number;
  primaryImageUrl: string | null;
  categoryName: string;
  categorySlug: string;
  isActive: boolean;
}

export interface ProductImageDto {
  id: string;
  url: string;
  altText: string | null;
  displayOrder: number;
  isPrimary: boolean;
}

export interface ProductVariantDto {
  id: string;
  color: string;
  size: string;
  sku: string;
  price: number;
  isActive: boolean;
  availableQuantity: number;
}

export interface ProductDetailDto {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  price: number;
  isActive: boolean;
  categoryName: string;
  categorySlug: string;
  images: ProductImageDto[];
  variants: ProductVariantDto[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateCategoryRequest {
  name: string;
  slug: string;
  description: string | null;
  displayOrder: number;
}

export interface CreateProductVariantRequest {
  color: string;
  size: string;
  sku: string;
  priceOverride: number | null;
  initialQuantity: number;
}

export interface CreateProductRequest {
  categoryId: string;
  name: string;
  slug: string;
  description: string | null;
  price: number;
  variants: CreateProductVariantRequest[];
}

export interface InventoryDto {
  productVariantId: string;
  sku: string;
  availableQuantity: number;
  reservedQuantity: number;
  soldQuantity: number;
  returnedQuantity: number;
  damagedQuantity: number;
}

export interface RestockRequest {
  productVariantId: string;
  quantity: number;
  reason: string | null;
}

export interface AdjustInventoryRequest {
  productVariantId: string;
  quantityDelta: number;
  reason: string;
}

export type DeliveryType = 'HomeDelivery' | 'StopDesk';

export type OrderStatus =
  | 'PendingConfirmation'
  | 'Confirmed'
  | 'Preparing'
  | 'ReadyToShip'
  | 'Shipped'
  | 'OutForDelivery'
  | 'Delivered'
  | 'Cancelled'
  | 'CustomerUnreachable'
  | 'DeliveryFailed'
  | 'Refused'
  | 'Returned';

export type PaymentStatus = 'Pending' | 'Collected' | 'Failed' | 'Refunded';

export interface OrderItemRequest {
  productVariantId: string;
  quantity: number;
}

export interface CreateOrderRequest {
  firstName: string;
  lastName: string;
  phone: string;
  wilaya: string;
  commune: string;
  address: string;
  deliveryType: DeliveryType;
  notes: string | null;
  items: OrderItemRequest[];
  couponCode?: string | null;
}

export interface OrderItemDto {
  id: string;
  productVariantId: string;
  productName: string;
  color: string;
  size: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface OrderStatusHistoryDto {
  id: string;
  oldStatus: OrderStatus;
  newStatus: OrderStatus;
  reason: string | null;
  createdAtUtc: string;
}

export type CallAttemptResult = 'NoAnswer' | 'Confirmed' | 'Cancelled' | 'CallbackScheduled';

export interface OrderCallAttemptDto {
  id: string;
  attemptNumber: number;
  result: CallAttemptResult;
  notes: string | null;
  calledAtUtc: string;
  nextCallAtUtc: string | null;
}

export interface RecordCallAttemptRequest {
  result: CallAttemptResult;
  notes: string | null;
  nextCallAt: string | null;
}

export interface OrderPromotionDto {
  id: string;
  promotionId: string | null;
  promotionName: string;
  discountAmount: number;
}

export interface OrderDetailDto {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  firstName: string;
  lastName: string;
  phone: string;
  wilaya: string;
  commune: string;
  address: string;
  deliveryType: DeliveryType;
  notes: string | null;
  paymentMethod: string;
  paymentStatus: PaymentStatus;
  subtotal: number;
  shippingCost: number;
  discountTotal: number;
  total: number;
  createdAtUtc: string;
  items: OrderItemDto[];
  statusHistory: OrderStatusHistoryDto[];
  callAttempts: OrderCallAttemptDto[];
  appliedPromotions: OrderPromotionDto[];
}

export interface OrderSummaryDto {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  customerFullName: string;
  phone: string;
  wilaya: string;
  total: number;
  createdAtUtc: string;
}

export interface ChangeOrderStatusRequest {
  newStatus: OrderStatus;
  reason: string | null;
}

// --- Auth ---

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface CurrentUserResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: CurrentUserResponse;
}

// --- Promotions ---

export type PromotionType =
  | 'ProductDiscount'
  | 'CategoryDiscount'
  | 'FlashSale'
  | 'PercentageDiscount'
  | 'FixedAmountDiscount'
  | 'BuyXGetY'
  | 'FreeShipping'
  | 'Coupon';

export interface PromotionDto {
  id: string;
  name: string;
  description: string | null;
  type: PromotionType;
  percentageValue: number | null;
  fixedAmountValue: number | null;
  buyQuantity: number | null;
  getQuantity: number | null;
  hasCouponCode: boolean;
  startsAtUtc: string;
  endsAtUtc: string;
  isActive: boolean;
  priority: number;
}

export interface PromotionDetailDto {
  id: string;
  name: string;
  description: string | null;
  type: PromotionType;
  percentageValue: number | null;
  fixedAmountValue: number | null;
  buyQuantity: number | null;
  getQuantity: number | null;
  couponCode: string | null;
  startsAtUtc: string;
  endsAtUtc: string;
  isActive: boolean;
  priority: number;
  productIds: string[];
  categoryIds: string[];
}

export interface SavePromotionRequest {
  name: string;
  description: string | null;
  type: PromotionType;
  percentageValue: number | null;
  fixedAmountValue: number | null;
  buyQuantity: number | null;
  getQuantity: number | null;
  couponCode: string | null;
  startsAtUtc: string;
  endsAtUtc: string;
  isActive: boolean;
  priority: number;
  productIds: string[];
  categoryIds: string[];
}
