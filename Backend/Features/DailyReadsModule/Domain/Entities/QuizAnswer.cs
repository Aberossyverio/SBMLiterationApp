using PureTCOWebApp.Core.Models;

namespace PureTCOWebApp.Features.DailyReadsModule.Domain.Entities;

public class QuizAnswer : AuditableEntity
{
    public int UserId { get; protected set; }
    public int DailyReadId { get; protected set; }
    public int QuestionSeq { get; protected set; }
    public string Answer { get; protected set; }

#pragma warning disable CS8618
    public QuizAnswer() { }
#pragma warning restore CS8618

    public static QuizAnswer Create(int userId, int dailyReadId, int questionSeq, string answer)
    {
        return new QuizAnswer
        {
            UserId = userId,
            DailyReadId = dailyReadId,
            QuestionSeq = questionSeq,
            Answer = answer
        };
    }

    public void UpdateAnswer(string answer)
    {
        Answer = answer;
    }
}
