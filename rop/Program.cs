using System;
using System.Linq;

// follow-along code for Scott Wlaschin's ROP talk here: https://vimeo.com/113707214
// full ROP page reference here: https://fsharpforfunandprofit.com/rop/
namespace RailwayOrientedProgramming
{
    static class Samples
    {
        public static void Main(string[] args)
        {
            Result<int> MaybeAdd3(double val) => val switch
            {
                13.0 => new Error("Unlucky!"),
                _ => (Result<int>)((int)val + 3)
            };

            float Subtract7(int val) => val - 7.0f;

            void PrintFloats(float value) => Console.WriteLine($"The current float value is currently {value}");

            Result<Half> MaybeInverse(float val) => val switch
            {
                0f => new Error("Inverse!"),
                _ => (Result<Half>)(Half)(1 / val)
            };

            void PrintFinalResult(Result<Half> result)
            {
                Action action = result.Item switch
                {
                    Half h => () => Console.WriteLine($"The final error path value is {h}"),
                    Error e => () => Console.WriteLine($"The final happy path value is {e}")
                };
                action();
            }

            var maybeAdd3        = (Func<double, Result<int>>)MaybeAdd3; // "raw" error-generating switch function
            var subtract7        = Map<int, float>(Subtract7); // map 1-track function
            var printFloats      = Audit<float>(PrintFloats); // audit float action (no return)
            var maybeInverse     = Bind<float, Half>(MaybeInverse); // bind error-generating inverse function
            var printFinalResult = Tee<Result<Half>>(PrintFinalResult); // tee action of 2-track action (no return)

            var compositeFunc = maybeAdd3
                .Compose(subtract7)
                .Compose(printFloats)
                .Compose(maybeInverse)
                .Compose(printFinalResult);

            Console.WriteLine("\nSingle output example: ");
            compositeFunc(13.0);

            Console.WriteLine("\nEnumerable output:\n");
            var results = new double[20]
                .Select((d, i) => (double)i) // 0,1,2,...,19
                .Select(compositeFunc) // perform composite operation
                .ToArray(); // force enumeration
        }

        static Func<T1, T3> Compose<T1, T2, T3>(this Func<T1, T2> func1, Func<T2, T3> func2)
        {
            return value => func2(func1(value));
        }                

        static Func<Result<TIn>, Result<TOut>> Bind<TIn, TOut>(this Func<TIn, Result<TOut>> func)
        {
            return choice => choice.Item switch
            {
                Error e => (Result<TOut>)e, // ugly cast because C#
                TIn tIn => func(tIn).Item switch
                {
                    Error eOut => eOut,
                    TOut tOut  => tOut
                }
            };
        }

        static Func<Result<TIn>, Result<TOut>> Map<TIn, TOut>(this Func<TIn, TOut> func)
        {
            return Bind<TIn, TOut>(input => func(input));
        }

        static Func<TIn, TIn> Tee<TIn>(Action<TIn> action)
        {
            return (TIn input) => { action(input); return input; };
        }

        static Func<Result<TIn>, Result<TIn>> Audit<TIn>(Action<TIn> action)
        {
            return Map<TIn, TIn>(Tee(action));
        }
    }
}