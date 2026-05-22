namespace SharedKernel;

public static class Constants
{


    /// <summary>
    /// Well-known service identifiers used when composing error codes.
    /// Assign stable numeric IDs per service (0-999).
    /// Update these values to match your organizational mapping.
    /// </summary>
    public static class ServiceCodes
    {
        public const string Catalog = "01";
        public const string Basket = "02";
        public const string Discount = "03";
        public const string Ordering = "04";
        public const string ShoppingAggregator = "05";
        public const string ApiGateway = "06";
    }

    /// <summary>
    /// Common application-level error codes (0-9999) to combine with HTTP/status and service ids.
    /// Keep these stable and extend as needed; prefer small stringegers for readability.
    /// </summary>
    public static class CommonErrorCodes
    {
        public const string Unknown = "00";
        public const string Validation = "01";
        public const string NotFound = "02";
        public const string Conflict = "03";
        public const string Unauthorized = "04";
        public const string Forbidden = "05";
        public const string Timeout = "06";
        public const string DependencyFailure = "07";
        public const string IdempotencyConflict = "08";
    }
}