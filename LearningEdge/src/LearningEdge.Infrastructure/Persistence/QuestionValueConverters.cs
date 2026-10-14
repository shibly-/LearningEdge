using System.Text.Json;
using LearningEdge.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LearningEdge.Infrastructure.Persistence;

internal static class QuestionValueConverters
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ValueConverter<IReadOnlyCollection<Option>, string> Options { get; } = new(
        options => SerializeOptions(options),
        json => DeserializeOptions(json));

    public static ValueComparer<IReadOnlyCollection<Option>> OptionsComparer { get; } = new(
        (left, right) => SerializeOptions(left) == SerializeOptions(right),
        value => SerializeOptions(value).GetHashCode(StringComparison.Ordinal),
        value => DeserializeOptions(SerializeOptions(value)));

    public static ValueConverter<SingleChoiceAnswer, string> SingleChoice { get; } = new(
        answer => JsonSerializer.Serialize(answer.CorrectOption.Text, JsonOptions),
        json => new SingleChoiceAnswer(new Option(JsonSerializer.Deserialize<string>(json, JsonOptions) ?? string.Empty)));

    public static ValueComparer<SingleChoiceAnswer> SingleChoiceComparer { get; } = new(
        (left, right) => left!.CorrectOption.Text == right!.CorrectOption.Text,
        value => value.CorrectOption.Text.GetHashCode(StringComparison.Ordinal),
        value => new SingleChoiceAnswer(new Option(value.CorrectOption.Text)));

    public static ValueConverter<MultipleChoiceAnswer, string> MultipleChoice { get; } = new(
        answer => JsonSerializer.Serialize(answer.CorrectOptions.Select(option => option.Text).ToList(), JsonOptions),
        json => new MultipleChoiceAnswer(DeserializeOptions(json)));

    public static ValueComparer<MultipleChoiceAnswer> MultipleChoiceComparer { get; } = new(
        (left, right) => SerializeOptions(left!.CorrectOptions) == SerializeOptions(right!.CorrectOptions),
        value => SerializeOptions(value.CorrectOptions).GetHashCode(StringComparison.Ordinal),
        value => new MultipleChoiceAnswer(DeserializeOptions(SerializeOptions(value.CorrectOptions))));

    public static ValueConverter<TrueFalseAnswer, bool> TrueFalse { get; } = new(
        answer => answer.CorrectValue,
        value => new TrueFalseAnswer(value));

    public static ValueComparer<TrueFalseAnswer> TrueFalseComparer { get; } = new(
        (left, right) => left!.CorrectValue == right!.CorrectValue,
        value => value.CorrectValue.GetHashCode(),
        value => new TrueFalseAnswer(value.CorrectValue));

    private static string SerializeOptions(IReadOnlyCollection<Option>? options) =>
        JsonSerializer.Serialize(options?.Select(option => option.Text).ToList() ?? [], JsonOptions);

    private static IReadOnlyCollection<Option> DeserializeOptions(string json)
    {
        var texts = JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        return texts.Select(text => new Option(text)).ToList();
    }
}
