using System.Net;

namespace Calcusystem.Core.Result;

public record Success : IMaybe
{
    public HttpStatusCode StatusCode {get;} = HttpStatusCode.OK;
    public string ErrorMessage
    {
        get
        {
            throw new InvalidOperationException("Attempted to read ErrorMessage from a Success");
        }
    }
}

public record Success<TResult> : Success, IMaybe<TResult>
{
    public TResult Result {get;}
    
    public Success(TResult result)
    {
        Result = result;
    }
}

public record Failure : IMaybe
{
    public HttpStatusCode StatusCode {get;}
    public string ErrorMessage {get;}

    public Failure(HttpStatusCode statusCode, string errorMessage)
    {
        if ((int)statusCode < 400) throw new ArgumentOutOfRangeException(
            nameof(statusCode),
            "must be an error code, so must be 4XX or 5XX status");

        StatusCode = statusCode;
        ErrorMessage = errorMessage;
    }

    public Failure<TResult> AsType<TResult>()
    {
        return new Failure<TResult>(StatusCode, ErrorMessage);
    }
}

public record Failure<TResult> : Failure, IMaybe<TResult>
{
    public TResult Result
    {
        get
        {
            throw new InvalidOperationException("Attempted to get Result field from Failure");
        }
    }

    public Failure(HttpStatusCode statusCode, string errorMessage) : base(statusCode, errorMessage)
    { }
}

public interface IMaybe
{
    public HttpStatusCode StatusCode {get;}
    public string ErrorMessage {get;}
    public bool IsFailure => StatusCode != HttpStatusCode.OK;
}

public interface IMaybe<TResult> : IMaybe
{
    TResult Result {get;}
}