using System;

namespace MyPi;

public static class PiCalc
{
    /// <summary>
    /// This calculates the <c>arctan(1/x)</c>.
    /// </summary>
    /// <param name="x">the number to calculate from</param>
    /// <param name="fractionalLength">The precision behind the comma</param>
    /// <returns>the resulting value</returns>
    public static BigFloat InvArcTan(BigFloat x, int fractionalLength)
    {
        var invX = BigFloat.One.Divide(x, fractionalLength);
        var invX2 = invX * invX;
        var carry = invX;
        var next = invX;
        var step = 1;
        BigFloat raise;
        do
        {
            next = -(next * invX2);
            step += 2;
            raise = next.Divide(new BigFloat(step), fractionalLength);
            carry += raise;
            // Console.WriteLine($"iat[{step:#,#0}]: {carry}");
        }
        while (raise != BigFloat.Zero);
        return carry;
    }

    public static BigFloat Pi(int fractionalLength)
    {
        return InvArcTan(new BigFloat(5), fractionalLength).Multiply(16)
            - InvArcTan(new BigFloat(239), fractionalLength).Multiply(4);
    }
}