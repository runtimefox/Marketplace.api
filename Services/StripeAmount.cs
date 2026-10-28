namespace MyApi.Services;

internal static class StripeAmount
{
    private const decimal MinorUnitsPerMajor = 100m;

    public static long ToMinorUnits(decimal amount) =>
        decimal.ToInt64(decimal.Round(amount * MinorUnitsPerMajor, 0, MidpointRounding.AwayFromZero));

    public static decimal FromMinorUnits(long amount) => amount / MinorUnitsPerMajor;
}
