using FluentValidation;
using LearningEdge.Application.Common.Files;
using LearningEdge.Application.Models.Commands.Trainings;

namespace LearningEdge.Application.Models.Validators.Trainings;

public class CreateTrainingCommandValidator : AbstractValidator<CreateTrainingCommand>
{
    public CreateTrainingCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category id is required.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Training name is required.")
            .MaximumLength(100).WithMessage("Training name must not exceed 100 characters.");
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Training description must not exceed 500 characters.")
            .When(x => x.Description is not null);
    }
}

public class UpdateTrainingCommandValidator : AbstractValidator<UpdateTrainingCommand>
{
    public UpdateTrainingCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category id is required.");
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Training id is required.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Training name is required.")
            .MaximumLength(100).WithMessage("Training name must not exceed 100 characters.");
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Training description must not exceed 500 characters.")
            .When(x => x.Description is not null);
    }
}

public class UploadTrainingFilesCommandValidator : AbstractValidator<UploadTrainingFilesCommand>
{
    public UploadTrainingFilesCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category id is required.");
        RuleFor(x => x.TrainingId)
            .NotEmpty().WithMessage("Training id is required.");
        RuleFor(x => x.UploadedByUserId)
            .NotEmpty().WithMessage("Uploader user id is required.");

        RuleFor(x => x.Files)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("At least one file is required.")
            .Must(files => files.Count <= TrainingFileRules.MaxFilesPerUpload)
                .WithMessage($"No more than {TrainingFileRules.MaxFilesPerUpload} files can be uploaded at once.")
            .Must(files => files.Sum(file => file.Length) <= TrainingFileRules.MaxTotalBytes)
                .WithMessage($"Total upload size must not exceed {TrainingFileRules.MaxTotalBytes / (1024 * 1024)} MB.");

        RuleForEach(x => x.Files).ChildRules(file =>
        {
            file.RuleFor(f => f.FileName)
                .NotEmpty().WithMessage("File name is required.")
                .MaximumLength(TrainingFileRules.MaxFileNameLength)
                    .WithMessage($"File name must not exceed {TrainingFileRules.MaxFileNameLength} characters.")
                .Must(TrainingFileRules.IsAllowedExtension)
                    .WithMessage(f => $"File '{f.FileName}' is not allowed. Allowed types: {TrainingFileRules.AllowedExtensionsText}.");
            file.RuleFor(f => f.Length)
                .GreaterThan(0).WithMessage(f => $"File '{f.FileName}' is empty.")
                .LessThanOrEqualTo(TrainingFileRules.MaxFileBytes)
                    .WithMessage(f => $"File '{f.FileName}' exceeds {TrainingFileRules.MaxFileBytes / (1024 * 1024)} MB.");
        });
    }
}
