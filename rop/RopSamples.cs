using System;
using System.Linq;

// follow-along code for Scott Wlaschin's ROP talk here: https://vimeo.com/113707214
// full ROP page reference here: https://fsharpforfunandprofit.com/rop/

// 1) error-generating switch function
var maybeAdd3 = (int val) => val switch
{
    13 => new Error("Unlucky!"),
    _  => (Result<long>)(val + 3)
};

// 2) simple 1-track function
var subtract7 = (long val) => val - 7f;

// 3) 1-track "dead-end" action (no return)
Action<float> printFloats = val => Console.Write($"The value is currently {val}.\t");

// 4) error-generating inverse function
var maybeInverse = (float val) => val switch
{
    0f => new Error("Inverse!"),
    _  => (Result<double>)(1f / val)
};

// 5) 2-track dead-end action (no return)
var printFinalResult = (Result<double> result) =>
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

public class Error
{
    public Error(string message) => Message = message;
    public string Message { get; init; }
    public override string ToString() => Message;
}

public class Result<T> : Choice<T, Error>
{
    public static implicit operator Result<T>(T value) => new Result<T>(value);
    public static implicit operator Result<T>(Error error) => new Result<T>(error);

    public Result(T item) : base(item) { }
    public Result(Error item) : base(item) { }
}

public class Choice<A, B>
{
    public Choice(A item) { Item = item!; }
    public Choice(B item) { Item = item!; }

    public dynamic Item { get; }

    // equality and string representation delegated to member (Item)
    public override bool Equals(object? obj) => Item.Equals(obj);
    public override int GetHashCode() => Item.GetHashCode();
    public override string ToString() => Item.ToString();
}

public static class RopExtensions
{
    public static Func<T1, T3> Compose<T1, T2, T3>(this Func<T1, T2> func1, Func<T2, T3> func2) =>
        value => func2(func1(value));

    public static Func<Result<TIn>, Result<TOut>> Bind<TIn, TOut>(this Func<TIn, Result<TOut>> func) =>
        choice => choice.Item switch
        {
            Error e => e,
            TIn tIn => func(tIn).Item switch
            {
                Error eOut => eOut,
                TOut tOut  => tOut,
                _          => new Error($"Invalid argument")
            },
            _ => new Error($"Invalid argument")
        };

    public static Func<Result<TIn>, Result<TOut>> Map<TIn, TOut>(this Func<TIn, TOut> func) =>
        Bind<TIn, TOut>(input => func(input)); // implicit upcast of return type of func to Result<TOut>

    public static Func<TIn, TIn> Tee<TIn>(this Action<TIn> deadEndFunction) =>
        input => { deadEndFunction(input); return input; };

    // Removed, as it's the same signature as Tee (with Result<TIn> as the TIn type)
    // public static Func<Result<TIn>, Result<TIn>> Audit<TIn>(this Action<Result<TIn>> deadEndFunction) =>
    // input => { deadEndFunction(input); return input; }; // perform the action and return the input
}