namespace Common.SharedKernel.Logging;

internal static class StrictMaskingFields
{
    private static readonly HashSet<string> ObservabilityIdentityFields =
    [
        "id",
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
    ];

    private static readonly HashSet<string> CreditCardFields =
    [
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
    ];

    private static readonly HashSet<string> CredentialFields =
    [
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
    ];

    private static readonly HashSet<string> EmailFields =
    [
        "email",
        "emailaddress",
        "primaryemail",
        "contactemail",
        "useremail"
    ];

    private static readonly HashSet<string> PhoneFields =
    [
        "phone",
        "phonenumber",
        "mobile",
        "mobilenumber",
        "contactnumber",
        "telephone",
        "tel"
    ];

    public static bool IsCreditCardField(string key) => IsStrictField(key, CreditCardFields);

    public static bool IsCredentialField(string key) => IsStrictField(key, CredentialFields);

    public static bool IsEmailField(string key) => IsStrictField(key, EmailFields);

    public static bool IsPhoneField(string key) => IsStrictField(key, PhoneFields);

    public static bool IsObservabilityIdentityField(string key) => IsStrictField(key, ObservabilityIdentityFields);

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