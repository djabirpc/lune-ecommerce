namespace Ecommerce.Domain.Marketing;

/// <summary>
/// When the "Purchase" marketing conversion event (CLAUDE.md section 21) fires. COD orders are not
/// real sales the instant they're placed — many never get confirmed (unreachable, refused, ...) — so
/// ad platforms can be told about either moment, depending on what the business wants to optimize for.
/// </summary>
public enum PurchaseEventTrigger
{
    /// <summary>Fires client-side, immediately, right after the order is created (current/default behavior).</summary>
    OrderCreated,

    /// <summary>Fires server-side when the order is confirmed (status transitions to Confirmed, or is
    /// created already Confirmed via the admin phone-order flow) — the customer's browser is long gone
    /// by then, so this requires a real server-to-server call (Meta Conversions API / TikTok Events API).</summary>
    OrderConfirmed,
}
