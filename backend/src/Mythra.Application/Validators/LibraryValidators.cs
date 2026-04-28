using FluentValidation;
using Mythra.Application.Dtos.Libraries;

namespace Mythra.Application.Validators;

public sealed class CreateLibraryRequestValidator : AbstractValidator<CreateLibraryRequest>
{
    public CreateLibraryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(1, 80);
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Folders).NotNull();
        RuleForEach(x => x.Folders).NotEmpty();
    }
}

public sealed class UpdateLibraryRequestValidator : AbstractValidator<UpdateLibraryRequest>
{
    public UpdateLibraryRequestValidator()
    {
        When(x => x.Name is not null, () => RuleFor(x => x.Name!).Length(1, 80));
        When(x => x.PreferredLanguage is not null, () => RuleFor(x => x.PreferredLanguage!).Length(2, 10));
    }
}

public sealed class AddFolderRequestValidator : AbstractValidator<AddFolderRequest>
{
    public AddFolderRequestValidator()
    {
        RuleFor(x => x.Path).NotEmpty();
    }
}
