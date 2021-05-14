
namespace RailwayOrientedProgramming
{
    class Error
    {
        public Error(string message) => Message = message;
        public string Message { get; init; }
        public override string ToString() => Message;
    }

    class Result<T> : Choice<T, Error>
    {
        public static implicit operator Result<T>(T value) => new Result<T>(value);
        public static implicit operator Result<T>(Error error) => new Result<T>(error);

        public Result(T item) : base(item) { }
        public Result(Error item) : base(item) { }
    }

    class Choice<A, B>
    {
        public Choice(A item) { Item = item!; }
        public Choice(B item) { Item = item!; }

        public dynamic Item { get; }

        // equality and string representation delegated to member (Item)
        public override bool Equals(object? obj) => Item.Equals(obj);
        public override int GetHashCode() => Item.GetHashCode();
        public override string ToString() => Item.ToString();
    }
}
