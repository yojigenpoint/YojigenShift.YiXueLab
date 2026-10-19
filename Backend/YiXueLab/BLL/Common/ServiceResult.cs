namespace YojigenShift.YiXueLab.BLL.Common
{
    public class ServiceResult
    {
        public bool IsSuccess { get; init; }
        public string? ErrorMessage { get; init; }
        public ServiceErrorCode ErrorCode { get; init; }

        public static ServiceResult Success() => new() { IsSuccess = true, ErrorCode = ServiceErrorCode.None };

        public static ServiceResult Failure(string message, ServiceErrorCode code = ServiceErrorCode.ValidationError)
            => new() { IsSuccess = false, ErrorMessage = message, ErrorCode = code };
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; init; }

        public static ServiceResult<T> Success(T data)
            => new() { IsSuccess = true, Data = data, ErrorCode = ServiceErrorCode.None };

        public new static ServiceResult<T> Failure(string message, ServiceErrorCode code = ServiceErrorCode.ValidationError)
            => new() { IsSuccess = false, ErrorMessage = message, ErrorCode = code };
    }
}
