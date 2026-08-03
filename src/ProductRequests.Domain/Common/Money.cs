using ProductRequests.Domain.Exceptions;

namespace ProductRequests.Domain.Common;

public readonly record struct Money
{
    public Money(decimal amount, string currency)
    {
        if (amount <= 0)
        {
            throw new DomainException("INVALID_AMOUNT", "Amount must be greater than zero.");
        }

        string normalizedCurrency = currency?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalizedCurrency.Length != 3 || !normalizedCurrency.All(char.IsLetter))
        {
            throw new DomainException("CURRENCY_MISMATCH", "Currency must be a three-letter ISO code.");
        }

        Amount = amount;
        Currency = normalizedCurrency;
    }

    public decimal Amount { get; }
    public string Currency { get; }
}
