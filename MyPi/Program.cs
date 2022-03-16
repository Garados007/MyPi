using System;

namespace MyPi;

// See https://aka.ms/new-console-template for more information

public class Program
{
    public static void Main(string[] args)
    {
        // var a = new LongFloat(12345678);
        // var b = new LongFloat(87654321);
        // Console.WriteLine($"a       = {a}");
        // Console.WriteLine($"a << 1  = {a << 1}");
        // Console.WriteLine($"a >> 1  = {a >> 1}");
        // Console.WriteLine($"b       = {b}");
        // Console.WriteLine($"a + b   = {a + b}");
        // Console.WriteLine($"a - b   = {a - b}");
        // Console.WriteLine($"a * b   = {a * b}");
        // var c = new LongFloat(1234554321) >> 2;
        // Console.WriteLine($"c       = {c}");
        // Console.WriteLine($"c + b   = {c + b}");
        // Console.WriteLine($"c - b   = {c - b}");
        // Console.WriteLine($"c * b   = {c * b}");

        // Console.WriteLine($"1 / 3   = {LongFloat.One.Divide(new LongFloat(3), 40)}");
        // Console.WriteLine($"1 / 0.3 = {LongFloat.One.Divide(new LongFloat(3).Shift(1) * 10, 40)}");
        // Console.WriteLine($"1 / 300 = {LongFloat.One.Divide(new LongFloat(300), 40)}");

        // Console.WriteLine($"cut 2   = {LongFloat.One.Divide(new LongFloat(3), 40).Cut(2)}");

        // Console.WriteLine(); Console.WriteLine();
        // Console.WriteLine($"arctan(1/5) = {PiCalc.InvArcTan(new LongFloat(5), 10)}");
        // Console.WriteLine($"            =   0.19739555984988078");
        // Console.WriteLine();
        Console.WriteLine($"my pi         = {PiCalc.Pi(100)}");
        Console.WriteLine($"wolfram alpha =   3.14159265358979323846264338327950288419716939937510582097494459230781640628620899862803482534211706798214808651328230664709384460955058223172535940812848111745028410270193852110555964462294895493038196442881097566593344612847564823378678316527120190914564856");
    }
}