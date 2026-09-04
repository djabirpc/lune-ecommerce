export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  roles: string[];
  createdAtUtc: string;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  isActive: boolean;
  roles: string[];
}

export interface ResetPasswordRequest {
  newPassword: string;
}

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
  costPrice: number | null;
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
  categoryId: string;
  categoryName: string;
  categorySlug: string;
  images: ProductImageDto[];
  variants: ProductVariantDto[];
  facebookPixelId: string | null;
  tikTokPixelId: string | null;
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

export interface UpdateCategoryRequest {
  name: string;
  slug: string;
  description: string | null;
  isActive: boolean;
  displayOrder: number;
}

export interface CreateProductVariantRequest {
  color: string;
  size: string;
  sku: string;
  priceOverride: number | null;
  costPrice: number | null;
  initialQuantity: number;
}

export interface CreateProductRequest {
  categoryId: string;
  name: string;
  slug: string;
  description: string | null;
  price: number;
  variants: CreateProductVariantRequest[];
  facebookPixelId?: string | null;
  tikTokPixelId?: string | null;
}

export interface UpdateProductRequest {
  categoryId: string;
  name: string;
  slug: string;
  description: string | null;
  price: number;
  isActive: boolean;
  facebookPixelId?: string | null;
  tikTokPixelId?: string | null;
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
  supplierId?: string | null;
  unitCost?: number | null;
}

export interface AdjustInventoryRequest {
  productVariantId: string;
  quantityDelta: number;
  reason: string;
}

export interface InventoryTransactionDto {
  id: string;
  productVariantId: string;
  type: string;
  quantity: number;
  reason: string | null;
  supplierId: string | null;
  supplierName: string | null;
  unitCost: number | null;
  createdAtUtc: string;
}

// --- Suppliers ---

export interface SupplierDto {
  id: string;
  name: string;
  phone: string | null;
  email: string | null;
  address: string | null;
  notes: string | null;
  isActive: boolean;
}

export interface SaveSupplierRequest {
  name: string;
  phone: string | null;
  email: string | null;
  address: string | null;
  notes: string | null;
  isActive: boolean;
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

export interface MarketingAttribution {
  utmSource?: string;
  utmMedium?: string;
  utmCampaign?: string;
  utmContent?: string;
  utmTerm?: string;
  fbclid?: string;
  ttclid?: string;
  referrer?: string;
  landingPage?: string;
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
  marketingAttribution?: MarketingAttribution | null;
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

export type ShippingCarrier = 'Fake' | 'Yalidine' | 'ZRExpress';

export type NormalizedShippingStatus =
  | 'Created'
  | 'PickedUp'
  | 'InTransit'
  | 'AtDestination'
  | 'OutForDelivery'
  | 'Delivered'
  | 'Failed'
  | 'Refused'
  | 'Returned'
  | 'Cancelled'
  | 'Unknown';

export interface ShipmentTrackingEventDto {
  id: string;
  providerStatus: string;
  normalizedStatus: NormalizedShippingStatus;
  description: string | null;
  occurredAtUtc: string;
}

export interface ShipmentDto {
  id: string;
  orderId: string;
  carrier: ShippingCarrier;
  providerShipmentId: string;
  trackingNumber: string | null;
  providerStatus: string;
  normalizedStatus: NormalizedShippingStatus;
  createdAtUtc: string;
  trackingEvents: ShipmentTrackingEventDto[];
}

export interface CreateShipmentRequest {
  carrier: ShippingCarrier;
}

export interface ShippingCarrierAvailabilityDto {
  carrier: ShippingCarrier;
  isConfigured: boolean;
  unavailableReason: string | null;
}

export interface ShippingRateDto {
  wilaya: string;
  homeDeliveryPrice: number;
  stopDeskPrice: number;
  isActive: boolean;
}

export interface UpdateShippingRateRequest {
  homeDeliveryPrice: number;
  stopDeskPrice: number;
  isActive: boolean;
}

export interface ShippingQuoteDto {
  wilaya: string;
  deliveryType: DeliveryType;
  price: number;
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
  returnReason: OrderReturnReason | null;
  createdAtUtc: string;
  items: OrderItemDto[];
  statusHistory: OrderStatusHistoryDto[];
  callAttempts: OrderCallAttemptDto[];
  appliedPromotions: OrderPromotionDto[];
  shipment: ShipmentDto | null;
  marketingAttribution: MarketingAttributionDto | null;
}

export interface MarketingAttributionDto {
  utmSource: string | null;
  utmMedium: string | null;
  utmCampaign: string | null;
  utmContent: string | null;
  utmTerm: string | null;
  fbclid: string | null;
  ttclid: string | null;
  referrer: string | null;
  landingPage: string | null;
}

export interface MarketingSourceSummaryDto {
  source: string;
  orderCount: number;
  totalRevenue: number;
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

export type OrderReturnReason = 'Damaged' | 'WrongSize' | 'WrongItem' | 'CustomerChangedMind' | 'Other';

export interface ChangeOrderStatusRequest {
  newStatus: OrderStatus;
  reason: string | null;
  returnReason?: OrderReturnReason | null;
}

export interface ReturnReasonSummaryDto {
  reason: OrderReturnReason;
  count: number;
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
