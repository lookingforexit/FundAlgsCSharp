using System.Numerics;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
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
        ushort[] leftHalfWords = SplitWordsToHalfWords(leftDigits);
        ushort[] rightHalfWords = SplitWordsToHalfWords(rightDigits);

        int spectrumSize = GetSpectrumSize(leftHalfWords.Length + rightHalfWords.Length);

        Complex[] leftSpectrum = BuildSpectrum(leftHalfWords, spectrumSize);
        Complex[] rightSpectrum = BuildSpectrum(rightHalfWords, spectrumSize);

        RunFft(leftSpectrum, false);
        RunFft(rightSpectrum, false);

        for (int i = 0; i < spectrumSize; i++)
        {
            leftSpectrum[i] *= rightSpectrum[i];
        }

        RunFft(leftSpectrum, true);

        long[] resultParts = ReadConvolution(leftSpectrum, leftHalfWords.Length + rightHalfWords.Length);
        NormalizeHalfWordCarries(resultParts, out long extraCarry);
        uint[] resultDigits = MergeHalfWordsToWords(resultParts, extraCarry);

        return SimpleMultiplier.Trim(resultDigits);
    }

    private static ushort[] SplitWordsToHalfWords(ReadOnlySpan<uint> digits)
    {
        ushort[] halfWords = new ushort[digits.Length * 2];

        for (int i = 0; i < digits.Length; i++)
        {
            halfWords[i * 2] = (ushort)(digits[i] & 0xFFFF);
            halfWords[i * 2 + 1] = (ushort)(digits[i] >> 16);
        }

        return halfWords;
    }

    private static int GetSpectrumSize(int requiredLength)
    {
        int size = 1;

        while (size < requiredLength)
        {
            size <<= 1;
        }

        return size;
    }

    private static Complex[] BuildSpectrum(ushort[] halfWords, int spectrumSize)
    {
        Complex[] spectrum = new Complex[spectrumSize];

        for (int i = 0; i < halfWords.Length; i++)
        {
            spectrum[i] = new Complex(halfWords[i], 0);
        }

        return spectrum;
    }

    private static long[] ReadConvolution(Complex[] spectrum, int resultLength)
    {
        long[] resultParts = new long[resultLength];

        for (int i = 0; i < resultLength - 1; i++)
        {
            resultParts[i] = (long)Math.Round(spectrum[i].Real);
        }

        return resultParts;
    }

    private static void NormalizeHalfWordCarries(long[] resultParts, out long extraCarry)
    {
        long carry = 0;

        for (int i = 0; i < resultParts.Length; i++)
        {
            long value = resultParts[i] + carry;
            resultParts[i] = value & 0xFFFF;
            carry = value >> 16;
        }

        extraCarry = carry;
    }

    private static uint[] MergeHalfWordsToWords(long[] halfWords, long extraCarry)
    {
        List<uint> resultDigits = [];

        for (int i = 0; i < halfWords.Length; i += 2)
        {
            uint lowPart = (uint)halfWords[i];
            uint highPart = i + 1 < halfWords.Length ? (uint)halfWords[i + 1] : 0;
            resultDigits.Add(lowPart | (highPart << 16));
        }

        while (extraCarry > 0)
        {
            uint lowPart = (uint)(extraCarry & 0xFFFF);
            extraCarry >>= 16;

            uint highPart = (uint)(extraCarry & 0xFFFF);
            extraCarry >>= 16;

            resultDigits.Add(lowPart | (highPart << 16));
        }

        return resultDigits.ToArray();
    }

    private static void RunFft(Complex[] values, bool invert)
    {
        int length = values.Length;

        for (int i = 1, j = 0; i < length; i++)
        {
            int bit = length >> 1;

            while ((j & bit) != 0)
            {
                j ^= bit;
                bit >>= 1;
            }

            j ^= bit;

            if (i < j)
            {
                (values[i], values[j]) = (values[j], values[i]);
            }
        }

        for (int blockLength = 2; blockLength <= length; blockLength <<= 1)
        {
            double angle = 2 * Math.PI / blockLength * (invert ? -1 : 1);
            Complex rootStep = Complex.FromPolarCoordinates(1, angle);

            for (int blockStart = 0; blockStart < length; blockStart += blockLength)
            {
                Complex root = Complex.One;

                for (int offset = 0; offset < blockLength / 2; offset++)
                {
                    Complex evenValue = values[blockStart + offset];
                    Complex oddValue = values[blockStart + offset + blockLength / 2] * root;

                    values[blockStart + offset] = evenValue + oddValue;
                    values[blockStart + offset + blockLength / 2] = evenValue - oddValue;
                    root *= rootStep;
                }
            }
        }

        if (invert)
        {
            for (int i = 0; i < length; i++)
            {
                values[i] /= length;
            }
        }
    }

    private static bool IsZeroDigits(ReadOnlySpan<uint> digits)
    {
        return digits.Length == 1 && digits[0] == 0;
    }
}
