using ApiFluentValidator.Models;
using FluentValidation;

namespace ApiFluentValidator.Validators;

public class TodoValidator : AbstractValidator<Todo>
{
    public TodoValidator()
    {
        RuleFor(m => m.Name).NotEmpty();
        RuleFor(m => m.IsComplete).NotEmpty();
        RuleFor(m => m.CompletedTimestamp).LessThanOrEqualTo(DateTime.Now);
    }
}
