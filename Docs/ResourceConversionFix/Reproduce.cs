using System;
using Weedtopia.Domain;

public static class Reproduce
{
    public static void Main()
    {
        var random=new Random(20260926);
        int failures=0;
        for(int i=0;i<1000000;i++)
        {
            var input=new Amount(1+random.NextDouble()*9,random.Next(-5,10));
            var output=input/2;
            var remaining=input-output*2;
            if(remaining<0)
            {
                if(failures++<8)Console.WriteLine("input="+input.Serialize()+" output="+output.Serialize()+" consumed="+(output*2).Serialize()+" remaining="+remaining.Serialize());
            }
        }
        Console.WriteLine("negativeRoundTrips="+failures);
    }
}
