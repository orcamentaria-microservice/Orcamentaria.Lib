using FluentValidation.Results;

namespace Orcamentaria.Lib.Domain.Validators
{
    public interface IValidatorEntity<T> where T : class
    {
        void CommonValidation(T entity);
        ValidationResult ValidateBeforeInsert(T entity);
        ValidationResult ValidateBeforeUpdate(T entity);
    }
}
