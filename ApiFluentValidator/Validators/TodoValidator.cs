using ApiFluentValidator.Models;
using FluentValidation;

namespace ApiFluentValidator.Validators;

public class TodoValidator : AbstractValidator<Todo>
{
    public TodoValidator()
    {
        RuleFor(m => m.Name).NotEmpty().WithMessage("The field 'Name' is required.");
        RuleFor(m => m.CompletedTimestamp).LessThanOrEqualTo(DateTime.Now);
    }
}
