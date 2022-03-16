using System;
using System.Text;
#if DEBUG
using static System.Diagnostics.Debug;
#endif

namespace MyPi;

public readonly struct BigFloat : IComparable<BigFloat>
{
    private readonly bool negate;
    private readonly ReadOnlyMemory<byte> digits;
    private readonly int fractionalLength;

    private int DecimalLength => digits.Length - fractionalLength;

    private BigFloat(bool negate, int fractionalLength, ReadOnlyMemory<byte> digits)
    {
        this.negate = negate;
        this.fractionalLength = fractionalLength;
        this.digits = digits;
    }

    public BigFloat(int value)
    {
        fractionalLength = 0;
        negate = value < 0;
        value = Math.Abs(value);
        Memory<byte> digits = new byte[5];
        int i = 4;
        for (; i >= 0 && value > 0; i--)
        {
            digits.Span[i] = (byte)(value % 100);
            value /= 100;
        }
        this.digits = digits[(i + 1)..];
    }

    public static BigFloat Zero { get; } = new BigFloat(false, 0, new byte[] { 0 });

    public static BigFloat One { get; } = new BigFloat(false, 0, new byte[] { 1 });

    public string ToString(System.Globalization.NumberFormatInfo format)
    {
        var span = digits.Span;
        var sb = new StringBuilder();
        sb.Append(negate ? format.NegativeSign : format.PositiveSign);
        var decimalLength = DecimalLength;
        int i = 0;
        for (; i < decimalLength; ++i)
            sb.Append(span[i].ToString("00"));
        if (fractionalLength <= 0)
            return sb.ToString();
        sb.Append(format.NumberDecimalSeparator);
        for (; i < span.Length; ++i)
            sb.Append(span[i].ToString("00"));
        return sb.ToString();
    }

    public override string ToString()
    {
        return ToString(System.Globalization.CultureInfo.CurrentCulture.NumberFormat);
    }

    public BigFloat Negate()
    {
        return new BigFloat(!negate, fractionalLength, digits);
    }
    
    public BigFloat Add(BigFloat second)
        => Add(second, true);

    private BigFloat Add(BigFloat second, bool reduce)
    {
        if (negate != second.negate)
            return Subtract(second.Negate());
        var decimalLength = Math.Max(DecimalLength, second.DecimalLength) + 1;
        var fractionalLength = Math.Max(this.fractionalLength, second.fractionalLength);
        Memory<byte> digits = new byte[decimalLength + fractionalLength];

        var leftOffset = decimalLength - this.DecimalLength;
        var rightOffset = decimalLength - second.DecimalLength;
        // add from right to left
        int carry = 0;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            var left = i >= leftOffset && i < leftOffset + this.digits.Length ? this.digits.Span[i - leftOffset] : 0;
            var right = i >= rightOffset && i < rightOffset + second.digits.Length ? second.digits.Span[i - rightOffset] : 0;

            var sum = left + right + carry;
            if (sum >= 100)
            {
                carry = sum / 100;
                sum -= carry * 100;
            }
            else carry = 0;
            digits.Span[i] = (byte)sum;
        }

        // in the end the carry should be 0
#if DEBUG
        Assert(carry == 0, "overflowing carry");
#endif

        var result = new BigFloat(negate, fractionalLength, digits);
        return reduce ? result.Shorten() : result;
    }

    public BigFloat Subtract(BigFloat second)
    {
        if (negate != second.negate)
            return Add(second.Negate());

        var comp = CompareTo(second);
        if (comp == 0)
            return Zero;
        if (comp < 0)
            return second.Subtract(this).Negate();
        
        var decimalLength = Math.Max(DecimalLength, second.DecimalLength);
        var fractionalLength = Math.Max(this.fractionalLength, second.fractionalLength);
        Memory<byte> digits = new byte[decimalLength + fractionalLength];

        var leftOffset = decimalLength - this.DecimalLength;
        var rightOffset = decimalLength - second.DecimalLength;
        // subtract from right to left
        int carry = 0;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            var left = i >= leftOffset && i < leftOffset + this.digits.Length ? this.digits.Span[i - leftOffset] : 0;
            var right = i >= rightOffset && i < rightOffset + second.digits.Length ? second.digits.Span[i - rightOffset] : 0;

            var sum = left - right - carry;
            if (sum < 0)
            {
                sum += 100;
                carry = 1;
            }
            else carry = 0;
            digits.Span[i] = (byte)sum;
        }

        // in the end the carry should be 0
#if DEBUG
        Assert(carry == 0, "overflowing carry");
#endif

        return new BigFloat(negate, fractionalLength, digits).Shorten();
    }

    public BigFloat Shift(int distance)
    {
        var fraction = fractionalLength + distance;
        if (fraction > this.digits.Length)
        {
            Memory<byte> digits = new byte[fraction];
            this.digits.CopyTo(digits[(fraction - this.digits.Length)..]);
            return new BigFloat(negate, fraction, digits);
        }
        if (fraction < 0)
        {
            Memory<byte> digits = new byte[this.digits.Length - fraction];
            this.digits.CopyTo(digits[..this.digits.Length]);
            return new BigFloat(negate, 0, digits);
        }
        return new BigFloat(negate, fraction, this.digits);
    }

    public BigFloat Multiply(int factor)
        => Multiply(new BigFloat(factor));

    public BigFloat Multiply(BigFloat second)
    {
        Memory<byte> digits = new byte[this.digits.Length + second.digits.Length + 1];

        for (int i = second.digits.Length - 1; i >= 0; i--)
        {
            var factor = second.digits.Span[i];
            int carry = 0;
            for (int j = this.digits.Length - 1; j >= 0; j--)
            {
                var pos = i + j + 2;
                var product = this.digits.Span[j] * factor + carry + digits.Span[pos];
                digits.Span[pos] = (byte)(product % 100);
                carry = product / 100;
            }
            carry += digits.Span[i + 1];
#if DEBUG
            Assert(carry < 100, $"too large carry (carry={carry}, i={i})");
#endif
            digits.Span[i + 1] = (byte)carry;
        }

        var result = new BigFloat(
            negate ^ second.negate,
            fractionalLength + second.fractionalLength,
            digits
        );
        return result.Shorten();
    }

    public BigFloat Divide(BigFloat divisor)
        => Divide(divisor, fractionalLength + divisor.fractionalLength);

    public BigFloat Divide(BigFloat divisor, int fractionalLength)
    {
        if (fractionalLength < 0)
            throw new ArgumentOutOfRangeException(nameof(fractionalLength));
        if (divisor.CompareTo(Zero) == 0)
            throw new DivideByZeroException();
        
        // we need a positive divisor
        if (divisor.negate)
        {
            return Negate().Divide(divisor.Negate(), fractionalLength);
        }
        
        // shift both sides so the fractional length of the divisor is zero
        if (divisor.fractionalLength > 0)
        {
            return Shift(-divisor.fractionalLength)
                .Divide(divisor.Shift(-divisor.fractionalLength), fractionalLength);
        }

        // to have a fractional part in the divident is troublesome. Lets cheat here.
        if (this.fractionalLength > 0)
        {
            var div1 = new BigFloat(negate, 0, this.digits);
            var div2 = divisor.Shift(-this.fractionalLength);
            var res = div1.Divide(div2, fractionalLength);
            res = res.Shift(this.fractionalLength);
            res = res.Cut(fractionalLength);
            return res;
        }

        // leading zeros will yield errors in the later execution
        if (divisor.digits.Length > 1 && divisor.digits.Span[0] == 0)
            divisor = divisor.Shorten();

        // if the divisor was many times smaller than the divident than the result number is large
        // as well. This will be remembers as the fraction shift is shifted to the negative.
        int fraction = divisor.digits.Length - this.digits.Length;

        Memory<byte> digits = new byte[fractionalLength + Math.Max(DecimalLength, Math.Abs(fraction))];

        // prepare the carry buffer
        Memory<byte> carry = new byte[divisor.digits.Length + 1];
        var commonDigitLength = Math.Min(this.digits.Length, divisor.digits.Length);
        this.digits[..commonDigitLength].CopyTo(carry[1..(commonDigitLength+1)]);
        int digit = 0;

        if (fraction > 0)
        {
            digit = fraction;
        }

        // now perform the long division until we have no fraction digits left
        for (; fraction < fractionalLength; ++fraction)
        {
            // get a rough estimation how much the divisor will fit in the carry. This is also an
            // upper bound for it.
            var factor = (carry.Span[0] * 100 + carry.Span[1]) / divisor.digits.Span[0];
            BigFloat multiplied;

            // now find the real factor
            int comp;
            do
            {
                comp = new BigFloat(false, 0, carry)
                    .CompareTo(multiplied = divisor.Multiply(factor));
                if (comp < 0)
                    factor--;
            }
            while (comp < 0);

            // factor is found. Insert it to the result field
            digits.Span[digit] = (byte)factor;
            digit++;

            // prepare the carry for the next step
            var next = new BigFloat(false, 0, carry);
            if (factor > 0)
                next = next.Subtract(multiplied);
            var copyLength = Math.Min(next.digits.Length, carry.Length - 1);
            next.digits[copyLength == next.digits.Length ? .. : 1..]
                .CopyTo(carry[(carry.Length - copyLength - 1)..^1]);
            carry[0..(carry.Length - copyLength - 1)].Span.Clear();
            if (digit + commonDigitLength < this.digits.Length)
                carry.Span[^1] = this.digits.Span[digit + commonDigitLength - 1];
            else carry.Span[^1] = 0;
        }

        // return a well formated number
        return new BigFloat(
            negate ^ divisor.negate,
            fractionalLength,
            digits
        ).Shorten();
    }

    /// <summary>
    /// This will cut the precision to a new length. The length can only go shorter and not longer
    /// </summary>
    /// <param name="fractionalLength">new fraction length</param>
    /// <returns>the cutted number</returns>
    public BigFloat Cut(int fractionalLength)
    {
        if (fractionalLength < 0)
            throw new ArgumentOutOfRangeException(nameof(fractionalLength));
        if (this.fractionalLength < fractionalLength)
            return this;
        var diff = this.fractionalLength - fractionalLength;
        return new BigFloat(negate, fractionalLength, digits[..^diff]);
    }

    private BigFloat Shorten()
    {
        int decimalLength = digits.Length - fractionalLength;
        int start = 0;
        for (; start < decimalLength - 1; ++start)
            if (digits.Span[start] != 0)
                break;
        int end = digits.Length;
        int fraction = fractionalLength;
        for (; end > decimalLength; --end)
            if (digits.Span[end - 1] != 0)
                break;
        fraction = end - decimalLength;
        return new BigFloat(negate, fraction, digits[start..end]);
    }

    public int CompareTo(BigFloat other)
    {
        if (negate && !other.negate)
            return -1;
        if (!negate && other.negate)
            return 1;

        var decimalLength = Math.Max(DecimalLength, other.DecimalLength);
        var fractionalLength = Math.Max(this.fractionalLength, other.fractionalLength);

        var leftOffset = decimalLength - this.DecimalLength;
        var rightOffset = decimalLength - other.DecimalLength;

        for (int i = 0; i < decimalLength + fractionalLength; ++i)
        {
            var left = i >= leftOffset && i < leftOffset + this.digits.Length ? this.digits.Span[i - leftOffset] : 0;
            var right = i >= rightOffset && i < rightOffset + other.digits.Length ? other.digits.Span[i - rightOffset] : 0;

            var comp = left.CompareTo(right);
            if (comp != 0)
                return comp;
        }

        return 0;
    }

    public override bool Equals(object? obj)
    {
        return obj is BigFloat @float && CompareTo(@float) == 0;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(negate, digits, fractionalLength, DecimalLength);
    }

    public static BigFloat operator +(BigFloat left, BigFloat right)
        => left.Add(right);

    public static BigFloat operator -(BigFloat left, BigFloat right)
        => left.Subtract(right);

    public static BigFloat operator -(BigFloat value)
        => value.Negate();
    
    public static BigFloat operator *(BigFloat left, BigFloat right)
        => left.Multiply(right);

    public static BigFloat operator *(BigFloat left, int right)
        => left.Multiply(right);

    public static BigFloat operator /(BigFloat left, BigFloat right)
        => left.Divide(right);
    
    public static BigFloat operator <<(BigFloat value, int distance)
        => value.Shift(-distance);
    
    public static BigFloat operator >>(BigFloat value, int distance)
        => value.Shift(distance);
    
    public static bool operator <(BigFloat left, BigFloat right)
        => left.CompareTo(right) < 0;

    public static bool operator <=(BigFloat left, BigFloat right)
        => left.CompareTo(right) <= 0;

    public static bool operator >(BigFloat left, BigFloat right)
        => left.CompareTo(right) > 0;

    public static bool operator >=(BigFloat left, BigFloat right)
        => left.CompareTo(right) >= 0;

    public static bool operator ==(BigFloat left, BigFloat right)
        => left.CompareTo(right) == 0;

    public static bool operator !=(BigFloat left, BigFloat right)
        => left.CompareTo(right) != 0;
}