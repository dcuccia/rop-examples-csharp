using System;
using System.Linq;
using RailwayOrientedProgramming;

// follow-along code for Scott Wlaschin's ROP talk here: https://vimeo.com/113707214
// full ROP page reference here: https://fsharpforfunandprofit.com/rop/

// 1) error-generating switch function
Func<int, Result<long>> maybeAdd3 = val => val switch
{
    13 => new Error("Unlucky!"),
    _  => (Result<long>)(val + 3)
};

// 2) simple 1-track function
Func<long, float> subtract7 = val => val - 7f;

// 3) 1-track "dead-end" action (no return)
Action<float> printFloats = val => Console.Write($"The value is currently {val}.\t");

// 4) error-generating inverse function
Func<float, Result<double>> maybeInverse = val => val switch
{
    0f => new Error("Inverse!"),
    _  => (Result<double>)(1f / val)
};

// 5) 2-track dead-end action (no return)
Action<Result<double>> printFinalResult = result =>
{
    Action action = result.Item switch
    {
        double d => () => Console.WriteLine($"Happy path! Final value is {d}"),
        Error e  => () => Console.WriteLine($"Error path :( ({e.Message})")
    };
    action();
};

var compositeFunc = maybeAdd3         // 1) error-generating function
    .Compose(subtract7.Map())         // 2) map a simple 1-track function
    .Compose(printFloats.Tee().Map()) // 3) tee and map a simple dead-end action
    .Compose(maybeInverse.Bind())     // 4) bind error-generating inverse function
    .Compose(printFinalResult.Tee()); // 5) tee a 2-track dead-end action

Console.WriteLine("\nOutput:\n");
var results = Enumerable.Range(0, 14) // 0,1,2,...,13
    .Select(compositeFunc)            // perform composite operation
    .ToArray();                       // force enumeration