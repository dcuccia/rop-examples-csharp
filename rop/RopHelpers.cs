
using System;

namespace RailwayOrientedProgramming
{
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
                Error e => (Result<TOut>)e, // ugly cast because C#
                TIn tIn => func(tIn).Item switch
                           {
                               Error eOut => eOut,
                               TOut tOut  => tOut,
                               _          => new Error($"Invalid argument")
                           },
                _       => new Error($"Invalid argument")
            };

        public static Func<Result<TIn>, Result<TOut>> Map<TIn, TOut>(this Func<TIn, TOut> func) =>
            Bind<TIn, TOut>(input => func(input)); // implicit upcast of return type of func to Result<TOut>

        public static Func<TIn, TIn> Tee<TIn>(this Action<TIn> deadEndFunction) =>
            input => { deadEndFunction(input); return input; };

        // Removed, as it's the same signature as Tee (with Result<TIn> as the TIn type)
        // public static Func<Result<TIn>, Result<TIn>> Audit<TIn>(this Action<Result<TIn>> deadEndFunction) =>
            // input => { deadEndFunction(input); return input; }; // perform the action and return the input
    }
}
