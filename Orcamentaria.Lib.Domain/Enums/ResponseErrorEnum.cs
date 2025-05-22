namespace Orcamentaria.Lib.Domain.Enums
{
    public enum ResponseErrorEnum
    {
        InvalidRequest = 1000,
        ValidationFailed = 1001,
        MissingRequiredField = 1002,
        InvalidFormat = 1003,
        Forbidden = 1200,
        AccessDenied = 1201,
        ResourceNotFound = 1300,
        Conflict = 1400,
        DuplicateRecord = 1401,
        BusinessRuleViolation = 1500,
        InternalError = 1600,
        UnexpectedError = 1601,
        DatabaseError = 1602,
        ExternalServiceFailure = 1603,
        NotFound = 404
    }
}
