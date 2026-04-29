using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    private const int SimpleThreshold = 32;

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
        if (leftDigits.Length == 0 || rightDigits.Length == 0)
        {
            return [0];
        }

        if (leftDigits.Length <= SimpleThreshold || rightDigits.Length <= SimpleThreshold)
        {
            return SimpleMultiplier.MultiplyDigits(leftDigits, rightDigits);
        }

        int middle = Math.Max(leftDigits.Length, rightDigits.Length) / 2;

        int leftLowLength = Math.Min(leftDigits.Length, middle);
        int rightLowLength = Math.Min(rightDigits.Length, middle);

        ReadOnlySpan<uint> leftLowPart = leftDigits[..leftLowLength];
        ReadOnlySpan<uint> leftHighPart = leftDigits[leftLowLength..];
        ReadOnlySpan<uint> rightLowPart = rightDigits[..rightLowLength];
        ReadOnlySpan<uint> rightHighPart = rightDigits[rightLowLength..];

        uint[] lowProduct = MultiplyDigits(leftLowPart, rightLowPart);
        uint[] highProduct = MultiplyDigits(leftHighPart, rightHighPart);

        uint[] leftSum = AddDigits(leftLowPart, leftHighPart);
        uint[] rightSum = AddDigits(rightLowPart, rightHighPart);
        uint[] crossProduct = MultiplyDigits(leftSum, rightSum);

        crossProduct = SubtractDigits(crossProduct, lowProduct);
        crossProduct = SubtractDigits(crossProduct, highProduct);

        uint[] highPart = ShiftWordsLeft(highProduct, middle * 2);
        uint[] middlePart = ShiftWordsLeft(crossProduct, middle);

        uint[] resultDigits = AddDigits(highPart, middlePart);
        resultDigits = AddDigits(resultDigits, lowProduct);

        return SimpleMultiplier.Trim(resultDigits);
    }

    private static uint[] AddDigits(ReadOnlySpan<uint> leftDigits, ReadOnlySpan<uint> rightDigits)
    {
        int maxLength = Math.Max(leftDigits.Length, rightDigits.Length);
        uint[] result = new uint[maxLength + 1];
        ulong carry = 0;

        for (int i = 0; i < maxLength; i++)
        {
            ulong sum = carry;

            if (i < leftDigits.Length)
            {
                sum += leftDigits[i];
            }

            if (i < rightDigits.Length)
            {
                sum += rightDigits[i];
            }

            result[i] = (uint)sum;
            carry = sum >> 32;
        }

        result[maxLength] = (uint)carry;
        return SimpleMultiplier.Trim(result);
    }

    private static uint[] SubtractDigits(ReadOnlySpan<uint> biggerDigits, ReadOnlySpan<uint> smallerDigits)
    {
        uint[] result = new uint[biggerDigits.Length];
        long borrow = 0;

        for (int i = 0; i < biggerDigits.Length; i++)
        {
            long difference = (long)biggerDigits[i] - borrow;

            if (i < smallerDigits.Length)
            {
                difference -= smallerDigits[i];
            }

            if (difference < 0)
            {
                difference += 1L << 32;
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }

            result[i] = (uint)difference;
        }

        return SimpleMultiplier.Trim(result);
    }

    private static uint[] ShiftWordsLeft(ReadOnlySpan<uint> digits, int wordsToShift)
    {
        if (IsZeroDigits(digits))
        {
            return [0];
        }

        uint[] result = new uint[digits.Length + wordsToShift];

        for (int i = 0; i < digits.Length; i++)
        {
            result[i + wordsToShift] = digits[i];
        }

        return result;
    }

    private static bool IsZeroDigits(ReadOnlySpan<uint> digits)
    {
        return digits.Length == 1 && digits[0] == 0;
    }
}
