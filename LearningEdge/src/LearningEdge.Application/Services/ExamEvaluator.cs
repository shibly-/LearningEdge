using LearningEdge.Domain.Entities.Exams;
using LearningEdge.Domain.Entities.Questions;

namespace LearningEdge.Application.Services;

public static class ExamEvaluator
{    
    public static int EvaluateResponse(Question question, object response)
    {
        return question switch
        {
            McqQuestion mcq => mcq.Answer.IsCorrect(response) ? mcq.Score : 0,
            TrueFalseQuestion tf => tf.Answer.IsCorrect(response) ? tf.Score : 0,
            MultipleSelectionQuestion ms => ms.Answer.IsCorrect(response) ? ms.Score : 0,
            _ => 0
        };
    }

    public static int EvaluateExam(Exam exam, Dictionary<Guid, object> responses)
    {
        int totalScore = 0;
        foreach (var question in exam.Questions)
        {
            if (responses.TryGetValue(question.Id, out var response))
            {
                totalScore += EvaluateResponse(question, response);
            }
        }
        return totalScore;
    }
        
    public static int CalculateMaxScore(Exam exam)
    {
        return exam.Questions.Sum(q => q.Score);
    }
}

