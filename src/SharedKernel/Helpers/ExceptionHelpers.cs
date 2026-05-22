#nullable enable

namespace SharedKernel.Helpers;

using System.Reflection;

internal static class ExceptionHelpers
{
    internal static string ResolveServiceCode(string? callerFilePath)
    {
        var normalizedPath = (callerFilePath ?? string.Empty).Replace('/', '\\').ToLowerInvariant();

        if (normalizedPath.Contains("\\src\\services\\catalog\\") || normalizedPath.Contains("\\tests\\catalog\\"))
            return Constants.ServiceCodes.Catalog;

        if (normalizedPath.Contains("\\src\\services\\basket\\") || normalizedPath.Contains("\\tests\\basket\\"))
            return Constants.ServiceCodes.Basket;

        if (normalizedPath.Contains("\\src\\services\\discount\\") || normalizedPath.Contains("\\tests\\discount\\"))
            return Constants.ServiceCodes.Discount;

        if (normalizedPath.Contains("\\src\\services\\ordering\\") || normalizedPath.Contains("\\tests\\ordering\\"))
            return Constants.ServiceCodes.Ordering;

        if (normalizedPath.Contains("\\src\\apigateways\\shopping.aggregator\\") || normalizedPath.Contains("\\tests\\shopping.aggregator\\"))
            return Constants.ServiceCodes.ShoppingAggregator;

        if (normalizedPath.Contains("\\src\\apigateways\\ocelotapigateway\\") || normalizedPath.Contains("\\tests\\ocelotapigateway\\"))
            return Constants.ServiceCodes.ApiGateway;

        return ResolveFromAssemblyName(Assembly.GetEntryAssembly()?.GetName().Name)
            ?? ResolveFromAssemblyName(Assembly.GetCallingAssembly().GetName().Name)
            ?? string.Empty;
    }

    internal static string ComposeCode(int httpStatus, string errorCode, string serviceCode)
    {
        var e = errorCode.Trim();
        var s = serviceCode.Trim();
        return $"{httpStatus}-{e}-{s}";
    }

    private static string? ResolveFromAssemblyName(string? assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
            return null;

        var lowered = assemblyName.Trim().ToLowerInvariant();

        if (lowered.Contains("catalog"))
            return Constants.ServiceCodes.Catalog;

        if (lowered.Contains("basket"))
            return Constants.ServiceCodes.Basket;

        if (lowered.Contains("discount"))
            return Constants.ServiceCodes.Discount;

        if (lowered.Contains("ordering"))
            return Constants.ServiceCodes.Ordering;

        if (lowered.Contains("shopping") || lowered.Contains("aggregator"))
            return Constants.ServiceCodes.ShoppingAggregator;

        if (lowered.Contains("gateway") || lowered.Contains("ocelot"))
            return Constants.ServiceCodes.ApiGateway;

        return null;
    }
}