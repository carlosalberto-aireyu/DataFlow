

namespace DataFlow.Core.Common
{
    public sealed class Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T? Value { get; }
        public string? Error { get; }


        private Result(bool isSuccess, T? value, string? error)
        {
            if (isSuccess && error != null)
                throw new InvalidOperationException("No se puede especificar un error para un resultado exitoso.");

            if (!isSuccess && value != null)
                throw new InvalidOperationException("No se puede especificar un valor para un resultado fallido.");


            IsSuccess = isSuccess;
            Value = value;
            Error = error;


        }

        public static Result<T> Success(T value) => new(true, value, null);
        public static Result<T> Failure(string error) => new(false, default, error);

    }
}
