#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace AKidsDream.Common.Results;

public static class Result
{
    public static Result<TValue, TError> Ok<TValue, TError>(TValue value) => Result<TValue, TError>.Ok(value);
    public static Result<TValue, TError> Fail<TValue, TError>(TError error) => Result<TValue, TError>.Fail(error);

    public static Result<TError> Ok<TError>() => Result<TError>.Ok();
    public static Result<TError> Fail<TError>(TError error) => Result<TError>.Fail(error);
}

public readonly record struct Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access Value on a failed Result. Error: {_error}");

    public TError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    private Result(TValue value)
    {
        IsSuccess = true;
        _value = value;
        _error = default;
    }

    private Result(TError error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public static Result<TValue, TError> Ok(TValue value) => new(value);
    public static Result<TValue, TError> Fail(TError error) => new(error);

    public bool TryGetValue(
        [NotNullWhen(true)] out TValue? value,
        [NotNullWhen(false)] out TError? error
    )
    {
        if (IsSuccess)
        {
            value = _value!;
            error = default;
            return true;
        }

        value = default;
        error = _error!;
        return false;
    }

    // -- HELPERS --
    
    // Match
    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<TError, TResult> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);

    public void Match(Action<TValue> onSuccess, Action<TError> onFailure)
    {
        if (IsSuccess) onSuccess(_value!);
        else onFailure(_error!);
    }
    
    // Map
    public Result<TNewValue, TError> Map<TNewValue>(Func<TValue, TNewValue> onSuccess)
    {
        return IsSuccess 
            ? Result.Ok<TNewValue, TError>(onSuccess(_value!))
            : Result.Fail<TNewValue, TError>(_error!);
    }

    public Result<TValue, TNewError> MapError<TNewError>(Func<TError, TNewError> onError)
    {
        return IsFailure 
            ? Result.Fail<TValue, TNewError>(onError(_error!))
            : Result.Ok<TValue, TNewError>(_value!);
    }
    
    // Bind
    public Result<TNewValue, TError> Bind<TNewValue>(
        Func<TValue, Result<TNewValue, TError>> onSuccess
    )
    {
        if (IsFailure)
            return Result.Fail<TNewValue, TError>(_error!);
        
        return onSuccess(_value!);
    }

    // Tap
    public Result<TValue, TError> TapSuccess(Action<TValue> action)
    {
        if (IsSuccess) action(_value!);
        return this;
    }

    public Result<TValue, TError> TapError(Action<TError> action)
    {
        if (IsFailure) action(_error!);
        return this;
    }
    
    // Ensure
    public Result<TValue, TError> Ensure(Func<bool> condition, TError error)
    {
        if (IsFailure) return this;
        if (!condition()) return Result.Fail<TValue, TError>(error);
        return this;
    }    
    
    public Result<TValue, TError> Ensure(Func<TValue, bool> condition, TError error)
    {
        if (IsFailure) return this;
        if (!condition(_value!)) return Result.Fail<TValue, TError>(error);
        return this;
    }

    public override string ToString() =>
        IsSuccess ? $"Success({_value})" : $"Failure({_error})";
}

public readonly record struct Result<TError>
{
    private readonly TError? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public TError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    private Result(bool isSuccess, TError? error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    public static Result<TError> Ok() => new(true, default);
    public static Result<TError> Fail(TError error) => new(false, error);

    // Match
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<TError, TResult> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(_error!);

    public void Match(Action onSuccess, Action<TError> onFailure)
    {
        if (IsSuccess) onSuccess();
        else onFailure(_error!);
    }
    
    // Map
    public Result<TNewError> Map<TNewError>(Func<TError, TNewError> onError) =>
        IsFailure ? Result.Fail(onError(_error!)) : Result.Ok<TNewError>();
    
    public override string ToString() =>
        IsSuccess ? "Success()" : $"Failure({_error})";
}

public static class ResultExtensions
{
    public static Result<TError> DropValue<TValue, TError>(this Result<TValue, TError> result) =>
        result.IsSuccess ? Result.Ok<TError>() : Result.Fail(result.Error);
}