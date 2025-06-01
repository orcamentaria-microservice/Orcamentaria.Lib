namespace Orcamentaria.Lib.Domain.Enums
{
    public enum ResponseErrorEnum
    {
        ValidationFailed = 400,
        Forbidden = 403,
        AccessDenied = 401,
        NotFound = 404,
        Conflict = 409,
        BusinessRuleViolation = 422,
        InternalError = 500,
        UnexpectedError = 503,
        DatabaseError = 501,
        ExternalServiceFailure = 502,
    }
}
