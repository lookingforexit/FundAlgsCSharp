using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit;
    private bool _isSmall;
    private uint _smallValue;
    private uint[]? _data;

    public bool IsNegative => _signBit == 1;

    public BetterBigInteger()
    {
        SetZero();
    }

    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        if (digits is null)
        {
            throw new ArgumentException("digits is null", nameof(digits));
        }

        if (digits.Length == 0)
        {
            throw new ArgumentException("digits array is empty", nameof(digits));
        }

        SetDigits(digits, isNegative);
    }

    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
    {
        if (digits is null)
        {
            throw new ArgumentException("digits is null", nameof(digits));
        }

        uint[] digitsArray = digits.ToArray();
        if (digitsArray.Length == 0)
        {
            throw new ArgumentException("digits array is empty", nameof(digits));
        }

        SetDigits(digitsArray, isNegative);
    }

    public BetterBigInteger(string value, int radix)
    {
        if (value is null)
        {
            throw new ArgumentException("value is null", nameof(value));
        }

        if (radix < 2 || radix > 36)
        {
            throw new ArgumentException("incorrect radix", nameof(radix));
        }

        SetZero();

        string text = value.Trim();
        if (text.Length == 0)
        {
            throw new ArgumentException("value is empty", nameof(value));
        }

        bool isNegative = false;
        int startIndex = 0;

        if (text[0] == '+' || text[0] == '-')
        {
            isNegative = text[0] == '-';
            startIndex = 1;
        }

        if (startIndex == text.Length)
        {
            throw new ArgumentException("no digits in value", nameof(value));
        }

        uint[] digits = [0];

        for (int i = startIndex; i < text.Length; i++)
        {
            int digit = CharToDigit(text[i]);
            if (digit >= radix)
            {
                throw new ArgumentException("digit does not fit radix", nameof(value));
            }

            digits = MultiplyDigitsByUInt(digits, (uint)radix);
            digits = AddUIntToDigits(digits, (uint)digit);
        }

        SetDigits(digits, isNegative);
    }

    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }

    public int CompareTo(IBigInteger? other)
    {
        if (other is null)
        {
            throw new ArgumentException("other is null", nameof(other));
        }

        if (IsNegative != other.IsNegative)
        {
            return IsNegative ? -1 : 1;
        }

        int absoluteCompare = CompareAbsolute(GetDigits(), other.GetDigits());
        return IsNegative ? -absoluteCompare : absoluteCompare;
    }

    public bool Equals(IBigInteger? other)
    {
        if (other is null)
        {
            return false;
        }

        if (IsNegative != other.IsNegative)
        {
            return false;
        }

        ReadOnlySpan<uint> leftDigits = GetDigits();
        ReadOnlySpan<uint> rightDigits = other.GetDigits();

        if (leftDigits.Length != rightDigits.Length)
        {
            return false;
        }

        for (int i = 0; i < leftDigits.Length; i++)
        {
            if (leftDigits[i] != rightDigits[i])
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_signBit);

        if (_isSmall)
        {
            hash.Add(_smallValue);
        }
        else
        {
            foreach (uint digit in _data!)
            {
                hash.Add(digit);
            }
        }

        return hash.ToHashCode();
    }

    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b) => a.Add(b);
    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b) => a.Subtract(b);
    public static BetterBigInteger operator -(BetterBigInteger a) => a.Negate();
    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b) => a.QuotientAndRemainder(b).quotient;
    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b) => a.QuotientAndRemainder(b).remainder;

    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        int maxLength = Math.Max(a.GetDigits().Length, b.GetDigits().Length);

        if (maxLength >= 2048)
        {
            return new FftMultiplier().Multiply(a, b);
        }

        if (maxLength >= 32)
        {
            return new KaratsubaMultiplier().Multiply(a, b);
        }

        return new SimpleMultiplier().Multiply(a, b);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        return -a - new BetterBigInteger([1]);
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
    {
        return ApplyBitwiseOperation(a, b, (left, right) => left & right);
    }

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
    {
        return ApplyBitwiseOperation(a, b, (left, right) => left | right);
    }

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
    {
        return ApplyBitwiseOperation(a, b, (left, right) => left ^ right);
    }

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        if (shift < 0)
        {
            throw new ArgumentException("shift is negative", nameof(shift));
        }

        if (a.IsZero())
        {
            return new BetterBigInteger();
        }

        if (shift == 0)
        {
            return new BetterBigInteger(a.GetDigitsArray(), a.IsNegative);
        }

        uint[] shiftedDigits = ShiftDigitsLeft(a.GetDigits(), shift);
        return new BetterBigInteger(shiftedDigits, a.IsNegative);
    }

    public static BetterBigInteger operator >>(BetterBigInteger a, int shift)
    {
        if (shift < 0)
        {
            throw new ArgumentException("shift is negative", nameof(shift));
        }

        if (a.IsZero())
        {
            return new BetterBigInteger();
        }

        if (shift == 0)
        {
            return new BetterBigInteger(a.GetDigitsArray(), a.IsNegative);
        }

        if (a.IsNegative)
        {
            BetterBigInteger absValue = -a;
            BetterBigInteger divisor = new BetterBigInteger([1]) << shift;
            BetterBigInteger adjustedValue = absValue + divisor - new BetterBigInteger([1]);
            return -(adjustedValue >> shift);
        }

        uint[] digits = a.GetDigitsArray();
        int wholeWords = shift / 32;
        int bitShift = shift % 32;

        if (wholeWords >= digits.Length)
        {
            return new BetterBigInteger();
        }

        int resultLength = digits.Length - wholeWords;
        uint[] result = new uint[resultLength];

        for (int i = wholeWords; i < digits.Length; i++)
        {
            int targetIndex = i - wholeWords;
            uint value = digits[i] >> bitShift;

            if (bitShift != 0 && i + 1 < digits.Length)
            {
                value |= digits[i + 1] << (32 - bitShift);
            }

            result[targetIndex] = value;
        }

        return new BetterBigInteger(TrimLeadingZeros(result), false);
    }

    public static bool operator ==(BetterBigInteger? a, BetterBigInteger? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(BetterBigInteger? a, BetterBigInteger? b) => !(a == b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;

    public override string ToString() => ToString(10);

    public string ToString(int radix)
    {
        if (radix < 2 || radix > 36)
        {
            throw new ArgumentException("incorrect radix", nameof(radix));
        }

        if (IsZero())
        {
            return "0";
        }

        uint[] digits = GetDigitsArray();
        List<char> chars = [];

        while (!IsZeroDigits(digits))
        {
            digits = DivideDigitsByUInt(digits, (uint)radix, out uint remainder);
            chars.Add(DigitToChar((int)remainder));
        }

        chars.Reverse();
        string result = new(chars.ToArray());
        return IsNegative ? "-" + result : result;
    }

    public bool IsZero()
    {
        return _data is null && _smallValue == 0;
    }

    public uint[] GetDigitsArray() => GetDigits().ToArray();

    private BetterBigInteger Add(BetterBigInteger other)
    {
        if (IsNegative == other.IsNegative)
        {
            uint[] sum = AddAbsolute(GetDigits(), other.GetDigits());
            return new BetterBigInteger(sum, IsNegative);
        }

        int comparison = CompareAbsolute(GetDigits(), other.GetDigits());
        if (comparison == 0)
        {
            return new BetterBigInteger();
        }

        if (comparison > 0)
        {
            uint[] leftDifference = SubtractAbsolute(GetDigits(), other.GetDigits());
            return new BetterBigInteger(leftDifference, IsNegative);
        }

        uint[] rightDifference = SubtractAbsolute(other.GetDigits(), GetDigits());
        return new BetterBigInteger(rightDifference, other.IsNegative);
    }

    private BetterBigInteger Subtract(BetterBigInteger other)
    {
        return Add(other.Negate());
    }

    private BetterBigInteger Negate()
    {
        if (IsZero())
        {
            return new BetterBigInteger();
        }

        return new BetterBigInteger(GetDigitsArray(), !IsNegative);
    }

    private (BetterBigInteger quotient, BetterBigInteger remainder) QuotientAndRemainder(BetterBigInteger other)
    {
        if (other.IsZero())
        {
            throw new DivideByZeroException();
        }

        DivideAbsolute(GetDigits(), other.GetDigits(), out uint[] quotientDigits, out uint[] remainderDigits);

        bool quotientIsNegative = !IsZeroDigits(quotientDigits) && IsNegative != other.IsNegative;
        bool remainderIsNegative = !IsZeroDigits(remainderDigits) && IsNegative;

        BetterBigInteger quotient = new(quotientDigits, quotientIsNegative);
        BetterBigInteger remainder = new(remainderDigits, remainderIsNegative);

        return (quotient, remainder);
    }

    private void SetZero()
    {
        _signBit = 0;
        _isSmall = true;
        _smallValue = 0;
        _data = null;
    }

    private void SetDigits(uint[] digits, bool isNegative)
    {
        uint[] normalizedDigits = TrimLeadingZeros(digits);

        if (normalizedDigits.Length == 1 && normalizedDigits[0] == 0)
        {
            SetZero();
            return;
        }

        if (normalizedDigits.Length == 1)
        {
            SetZero();
            _signBit = isNegative ? 1 : 0;
            _smallValue = normalizedDigits[0];
            return;
        }

        _signBit = isNegative ? 1 : 0;
        _isSmall = false;
        _smallValue = 0;
        _data = normalizedDigits;

        int length = _data.Length;
        while (length > 1 && _data[length - 1] == 0)
        {
            length--;
        }

        if (length != _data.Length)
        {
            Array.Resize(ref _data, length);
        }

        if (_data.Length == 1)
        {
            uint value = _data[0];
            SetZero();
            _smallValue = value;
        }
    }

    private uint[] ToTwosComplement(int length)
    {
        uint[] digits = GetDigitsArray();
        if (length < digits.Length)
        {
            length = digits.Length;
        }

        uint[] result = new uint[length];
        Array.Copy(digits, result, digits.Length);

        if (!IsNegative)
        {
            return result;
        }

        InvertWords(result);
        AddOne(result);

        return result;
    }

    private static BetterBigInteger FromTwosComplement(uint[] twosComplementDigits)
    {
        if (twosComplementDigits.Length == 0)
        {
            return new BetterBigInteger();
        }

        bool isNegative = (twosComplementDigits[^1] & 0x80000000) != 0;
        uint[] digits = (uint[])twosComplementDigits.Clone();

        if (isNegative)
        {
            InvertWords(digits);
            AddOne(digits);
        }

        digits = TrimLeadingZeros(digits);

        if (IsZeroDigits(digits))
        {
            return new BetterBigInteger();
        }

        return new BetterBigInteger(digits, isNegative);
    }

    private static BetterBigInteger ApplyBitwiseOperation(
        BetterBigInteger leftNumber,
        BetterBigInteger rightNumber,
        Func<uint, uint, uint> operation)
    {
        int length = Math.Max(leftNumber.GetDigits().Length, rightNumber.GetDigits().Length) + 2;

        uint[] leftWords = leftNumber.ToTwosComplement(length);
        uint[] rightWords = rightNumber.ToTwosComplement(length);
        uint[] result = new uint[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = operation(leftWords[i], rightWords[i]);
        }

        return FromTwosComplement(result);
    }

    private static int CompareAbsolute(ReadOnlySpan<uint> leftDigits, ReadOnlySpan<uint> rightDigits)
    {
        if (leftDigits.Length != rightDigits.Length)
        {
            return leftDigits.Length > rightDigits.Length ? 1 : -1;
        }

        for (int i = leftDigits.Length - 1; i >= 0; i--)
        {
            if (leftDigits[i] == rightDigits[i])
            {
                continue;
            }

            return leftDigits[i] > rightDigits[i] ? 1 : -1;
        }

        return 0;
    }

    private static uint[] AddAbsolute(ReadOnlySpan<uint> leftDigits, ReadOnlySpan<uint> rightDigits)
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
        return TrimLeadingZeros(result);
    }

    private static uint[] SubtractAbsolute(ReadOnlySpan<uint> biggerDigits, ReadOnlySpan<uint> smallerDigits)
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

        return TrimLeadingZeros(result);
    }

    private static void DivideAbsolute(
        ReadOnlySpan<uint> dividendDigits,
        ReadOnlySpan<uint> divisorDigits,
        out uint[] quotientDigits,
        out uint[] remainderDigits)
    {
        int comparison = CompareAbsolute(dividendDigits, divisorDigits);

        if (comparison < 0)
        {
            quotientDigits = [0];
            remainderDigits = dividendDigits.ToArray();
            return;
        }

        if (comparison == 0)
        {
            quotientDigits = [1];
            remainderDigits = [0];
            return;
        }

        int dividendBitLength = GetBitLength(dividendDigits);
        int divisorBitLength = GetBitLength(divisorDigits);

        quotientDigits = new uint[(dividendBitLength - divisorBitLength) / 32 + 1];
        remainderDigits = dividendDigits.ToArray();

        for (int shift = dividendBitLength - divisorBitLength; shift >= 0; shift--)
        {
            uint[] shiftedDivisor = ShiftDigitsLeft(divisorDigits, shift);

            if (CompareAbsolute(remainderDigits, shiftedDivisor) >= 0)
            {
                remainderDigits = SubtractAbsolute(remainderDigits, shiftedDivisor);
                SetBit(quotientDigits, shift);
            }
        }

        quotientDigits = TrimLeadingZeros(quotientDigits);
        remainderDigits = TrimLeadingZeros(remainderDigits);
    }

    private static int GetBitLength(ReadOnlySpan<uint> digits)
    {
        int lastIndex = digits.Length - 1;
        uint lastWord = digits[lastIndex];
        int bitsInLastWord = 32;

        while (bitsInLastWord > 0 && ((lastWord >> (bitsInLastWord - 1)) & 1) == 0)
        {
            bitsInLastWord--;
        }

        return lastIndex * 32 + bitsInLastWord;
    }

    private static void SetBit(uint[] digits, int bitIndex)
    {
        int wordIndex = bitIndex / 32;
        int bitInWord = bitIndex % 32;
        digits[wordIndex] |= 1u << bitInWord;
    }

    private static uint[] ShiftDigitsLeft(ReadOnlySpan<uint> digits, int shift)
    {
        if (shift == 0)
        {
            return digits.ToArray();
        }

        int wholeWords = shift / 32;
        int bitShift = shift % 32;
        uint[] result = new uint[digits.Length + wholeWords + 1];
        ulong carry = 0;

        for (int i = 0; i < digits.Length; i++)
        {
            ulong value = ((ulong)digits[i] << bitShift) | carry;
            result[i + wholeWords] = (uint)value;
            carry = value >> 32;
        }

        result[digits.Length + wholeWords] = (uint)carry;
        return TrimLeadingZeros(result);
    }

    private static uint[] MultiplyDigitsByUInt(ReadOnlySpan<uint> digits, uint multiplier)
    {
        if (multiplier == 0)
        {
            return [0];
        }

        if (multiplier == 1)
        {
            return digits.ToArray();
        }

        uint[] result = new uint[digits.Length + 1];
        ulong carry = 0;

        for (int i = 0; i < digits.Length; i++)
        {
            ulong product = (ulong)digits[i] * multiplier + carry;
            result[i] = (uint)product;
            carry = product >> 32;
        }

        result[^1] = (uint)carry;
        return TrimLeadingZeros(result);
    }

    private static uint[] AddUIntToDigits(ReadOnlySpan<uint> digits, uint valueToAdd)
    {
        uint[] result = digits.ToArray();
        ulong carry = valueToAdd;
        int index = 0;

        while (carry > 0 && index < result.Length)
        {
            ulong sum = result[index] + carry;
            result[index] = (uint)sum;
            carry = sum >> 32;
            index++;
        }

        if (carry == 0)
        {
            return TrimLeadingZeros(result);
        }

        uint[] extended = new uint[result.Length + 1];
        Array.Copy(result, extended, result.Length);
        extended[^1] = (uint)carry;
        return TrimLeadingZeros(extended);
    }

    private static uint[] DivideDigitsByUInt(ReadOnlySpan<uint> digits, uint divisor, out uint remainder)
    {
        uint[] result = new uint[digits.Length];
        ulong currentRemainder = 0;

        for (int i = digits.Length - 1; i >= 0; i--)
        {
            ulong currentValue = (currentRemainder << 32) | digits[i];
            result[i] = (uint)(currentValue / divisor);
            currentRemainder = currentValue % divisor;
        }

        remainder = (uint)currentRemainder;
        return TrimLeadingZeros(result);
    }

    private static uint[] TrimLeadingZeros(ReadOnlySpan<uint> digits)
    {
        int length = digits.Length;
        while (length > 1 && digits[length - 1] == 0)
        {
            length--;
        }

        return digits[..length].ToArray();
    }

    private static void InvertWords(uint[] digits)
    {
        for (int i = 0; i < digits.Length; i++)
        {
            digits[i] = ~digits[i];
        }
    }

    private static void AddOne(uint[] digits)
    {
        ulong carry = 1;
        for (int i = 0; i < digits.Length && carry != 0; i++)
        {
            ulong sum = digits[i] + carry;
            digits[i] = (uint)sum;
            carry = sum >> 32;
        }
    }

    private static bool IsZeroDigits(ReadOnlySpan<uint> digits)
    {
        return digits.Length == 1 && digits[0] == 0;
    }

    private static int CharToDigit(char c)
    {
        if (c >= '0' && c <= '9')
        {
            return c - '0';
        }

        if (c >= 'A' && c <= 'Z')
        {
            return c - 'A' + 10;
        }

        if (c >= 'a' && c <= 'z')
        {
            return c - 'a' + 10;
        }

        throw new ArgumentException("incorrect digit");
    }

    private static char DigitToChar(int digit)
    {
        return digit < 10
            ? (char)('0' + digit)
            : (char)('A' + digit - 10);
    }
}
