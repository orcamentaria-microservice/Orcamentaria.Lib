using FluentValidation.Results;

namespace Orcamentaria.Lib.Domain.Validators
{
    public interface IValidatorEntity<T> where T : class
    {
        ValidationResult ValidateBeforeInsert(T entity);
        ValidationResult ValidateBeforeUpdate(T entity);
    }
}
