namespace Common.SharedKernel.Logging;

internal static class StrictMaskingFields
{
    /// <summary>
    /// Critical field names that are excluded from masking by default because they are
    /// required for distributed tracing and correlation diagnostics.
    /// </summary>
    private static readonly HashSet<string> MaskingExcludedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "id",
        "eventtype",
        "correlationid",
        "xcorrelationid",
        "traceid",
        "xtraceid",
        "traceparent",
        "topic",
        "messageid",
        "routeTemplate",
        "requestpath",
        "requesturl",
        "endpoint",
        "path"
    };

    public static HashSet<string> CreateDefaultMaskingExcludedFields()
        => new(MaskingExcludedFields, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> CreditCardFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "cardnumber",
        "cardno",
        "creditcard",
        "ccnumber",
        "pan",
        "cvv",
        "cvc",
        "expiry",
        "expdate",
        "expiration",
        "expirationdate"
    };

    private static readonly HashSet<string> CredentialFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization",
        "token",
        "accesstoken",
        "refreshtoken",
        "idtoken",
        "apikey",
        "clientsecret",
        "password",
        "passwd",
        "pwd",
        "secret",
        "sessionid",
        "cookie",
        "setcookie",
        "authtoken",
        "jwttoken",
        "bearertoken"
    };

    private static readonly HashSet<string> EmailFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "email",
        "emailaddress",
        "primaryemail",
        "contactemail",
        "useremail"
    };

    private static readonly HashSet<string> PhoneFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "phone",
        "phonenumber",
        "mobile",
        "mobilenumber",
        "contactnumber",
        "telephone",
        "tel"
    };

    public static bool IsCreditCardField(string key) => IsStrictField(key, CreditCardFields);

    public static bool IsCredentialField(string key) => IsStrictField(key, CredentialFields);

    public static bool IsEmailField(string key) => IsStrictField(key, EmailFields);

    public static bool IsPhoneField(string key) => IsStrictField(key, PhoneFields);

    /// <summary>
    /// Returns <c>true</c> when the field should bypass masking.
    /// </summary>
    public static bool IsMaskingExcludedField(string key) => IsStrictField(key, MaskingExcludedFields);

    /// <summary>
    /// Returns <c>true</c> when the field should bypass masking using a configured exclusion set.
    /// </summary>
    public static bool IsMaskingExcludedField(string key, IReadOnlySet<string> excludedFields)
        => IsStrictField(key, excludedFields);

    private static bool IsStrictField(string key, IReadOnlySet<string> strictFields)
    {
        string normalizedKey = NormalizeKey(key);
        if (strictFields.Contains(normalizedKey))
        {
            return true;
        }

        int dotIndex = key.LastIndexOf('.');
        string terminal = dotIndex >= 0 ? key[(dotIndex + 1)..] : key;
        return strictFields.Contains(NormalizeKey(terminal));
    }

    private static string NormalizeKey(string key)
        => new(key.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}