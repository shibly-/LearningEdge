using LearningEdge.Application.Common.Files;
using LearningEdge.Application.Models.Commands.Trainings;
using LearningEdge.Application.Models.Validators.Trainings;

namespace LearningEdge.Tests.Application.Validators;

public class UploadTrainingFilesCommandValidatorTests
{
    private readonly UploadTrainingFilesCommandValidator _validator = new();

    [Theory]
    [InlineData("guide.pdf")]
    [InlineData("handout.DOCX")]
    [InlineData("notes.txt")]
    public void AllowsSupportedExtensions(string fileName)
    {
        var result = _validator.Validate(Command(File(fileName, 10)));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("setup.exe")]
    [InlineData("legacy.doc")]
    [InlineData("noextension")]
    [InlineData("guide.pdf.exe")]
    public void RejectsUnsupportedExtensions(string fileName)
    {
        var result = _validator.Validate(Command(File(fileName, 10)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("is not allowed"));
    }

    [Fact]
    public void RejectsMissingFiles()
    {
        var result = _validator.Validate(Command());

        Assert.Contains(result.Errors, error => error.ErrorMessage == "At least one file is required.");
    }

    [Fact]
    public void RejectsEmptyAndOversizedFiles()
    {
        var result = _validator.Validate(Command(
            File("empty.txt", 0),
            File("huge.pdf", TrainingFileRules.MaxFileBytes + 1)));

        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("'empty.txt' is empty"));
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("'huge.pdf' exceeds"));
    }

    [Fact]
    public void RejectsTooManyFiles()
    {
        var files = Enumerable.Range(0, TrainingFileRules.MaxFilesPerUpload + 1)
            .Select(index => File($"file{index}.txt", 10))
            .ToArray();

        var result = _validator.Validate(Command(files));

        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("No more than"));
    }

    private static UploadTrainingFilesCommand Command(params TrainingFileUpload[] files) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), files);

    private static TrainingFileUpload File(string name, long length) =>
        new(name, "application/octet-stream", length, Stream.Null);
}
