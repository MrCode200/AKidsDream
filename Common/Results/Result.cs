#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace AKidsDream.Common.Results;


public static class Result
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<TValue, TError> Ok<TValue, TError>(TValue value) => Result<TValue, TError>.Ok(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<TValue, TError> Fail<TValue, TError>(TError error) => Result<TValue, TError>.Fail(error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<TError> Ok<TError>() => Result<TError>.Ok();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<TValue, TError> Ok(TValue value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<TValue, TError> Fail(TError error) => new(error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result<TValue, TError>(TValue value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result<TValue, TError>(TError error) => new(error);

    public bool TryGetValue([NotNullWhen(true)] out TValue? value, [NotNullWhen(false)] out TError? error)
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

    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<TError, TResult> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);

    public void Match(Action<TValue> onSuccess, Action<TError> onFailure)
    {
        if (IsSuccess) onSuccess(_value!);
        else onFailure(_error!);
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<TError> Ok() => new(true, default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<TError> Fail(TError error) => new(false, error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result<TError>(TError error) => new(false, error);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<TError, TResult> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(_error!);

    public void Match(Action onSuccess, Action<TError> onFailure)
    {
        if (IsSuccess) onSuccess();
        else onFailure(_error!);
    }

    public override string ToString() =>
        IsSuccess ? "Success()" : $"Failure({_error})";
}
