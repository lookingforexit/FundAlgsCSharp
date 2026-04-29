using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        uint[] leftDigits = a.GetDigits().ToArray();
        uint[] rightDigits = b.GetDigits().ToArray();

        if (IsZeroDigits(leftDigits) || IsZeroDigits(rightDigits))
        {
            return new BetterBigInteger([0]);
        }

        uint[] resultDigits = MultiplyDigits(leftDigits, rightDigits);
        bool isNegative = a.IsNegative != b.IsNegative;
        return new BetterBigInteger(resultDigits, isNegative);
    }

    internal static uint[] MultiplyDigits(ReadOnlySpan<uint> leftDigits, ReadOnlySpan<uint> rightDigits)
    {
        uint[] result = new uint[leftDigits.Length + rightDigits.Length];

        for (int i = 0; i < leftDigits.Length; i++)
        {
            ulong carry = 0;

            for (int j = 0; j < rightDigits.Length; j++)
            {
                ulong current = result[i + j];
                current += (ulong)leftDigits[i] * rightDigits[j];
                current += carry;

                result[i + j] = (uint)current;
                carry = current >> 32;
            }

            int index = i + rightDigits.Length;
            while (carry > 0)
            {
                ulong current = result[index] + carry;
                result[index] = (uint)current;
                carry = current >> 32;
                index++;
            }
        }

        return Trim(result);
    }

    internal static uint[] Trim(ReadOnlySpan<uint> digits)
    {
        int length = digits.Length;
        while (length > 1 && digits[length - 1] == 0)
        {
            length--;
        }

        return digits[..length].ToArray();
    }

    private static bool IsZeroDigits(ReadOnlySpan<uint> digits)
    {
        return digits.Length == 1 && digits[0] == 0;
    }
}
