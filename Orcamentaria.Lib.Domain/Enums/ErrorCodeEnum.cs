namespace Orcamentaria.Lib.Domain.Enums
{
    public enum ErrorCodeEnum
    {
        Unauthorized = 401,
        NotFound = 404,
        Conflict = 409,
        ValidationFailed = 422,
        InternalError = 500,
        UnexpectedError = 503,
        DatabaseError = 501,
        ExternalServiceFailure = 502,
    }
}
