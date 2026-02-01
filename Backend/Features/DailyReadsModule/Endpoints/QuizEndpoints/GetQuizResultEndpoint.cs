using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using PureTCOWebApp.Core;
using PureTCOWebApp.Core.Models;
using PureTCOWebApp.Data;

namespace PureTCOWebApp.Features.DailyReadsModule.Endpoints.QuizEndpoints;

public record QuizResultDto(
    int QuestionSeq,
    string Question,
    string UserAnswer,
    bool IsCorrect
);

public record GetQuizResultResponse(
    int TotalQuestions,
    int CorrectAnswers,
    List<QuizResultDto> Results
);

public class GetQuizResultEndpoint(ApplicationDbContext dbContext)
    : EndpointWithoutRequest<ApiResponse<GetQuizResultResponse>>
{
    public override void Configure()
    {
        Get("{dailyReadId}/quiz/result");
        Group<DailyReadsEndpointGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var dailyReadId = Route<int>("dailyReadId");
        var userId = int.Parse(User.FindFirst("sub")!.Value);

        var questions = await dbContext.QuizQuestions
            .Where(q => q.DailyReadId == dailyReadId)
            .OrderBy(q => q.QuestionSeq)
            .ToListAsync(ct);

        if (!questions.Any())
        {
            await Send.ResultAsync(TypedResults.NotFound<ApiResponse>(
                Result.Failure(new Error("Quiz.NotFound", "No quiz questions found for this daily read"))
            ));
            return;
        }

        var userAnswers = await dbContext.QuizAnswers
            .Where(a => a.UserId == userId && a.DailyReadId == dailyReadId)
            .ToListAsync(ct);

        var results = new List<QuizResultDto>();
        var correctCount = 0;

        foreach (var question in questions)
        {
            var userAnswer = userAnswers.FirstOrDefault(a => a.QuestionSeq == question.QuestionSeq);
            var userAnswerText = userAnswer?.Answer ?? "";
            var isCorrect = userAnswerText.Equals(question.CorrectAnswer, StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
                correctCount++;

            results.Add(new QuizResultDto(
                question.QuestionSeq,
                question.Question,
                userAnswerText,
                isCorrect
            ));
        }

        var totalQuestions = questions.Count;

        var response = new GetQuizResultResponse(
            totalQuestions,
            correctCount,
            results
        );

        await Send.OkAsync(Result.Success(response), cancellation: ct);
    }
}
