using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizOnline.Data;
using QuizOnline.Models;

namespace QuizOnline.Controllers; 

[Authorize]
public class QuizTakingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public QuizTakingController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

   
    public async Task<IActionResult> Start(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .AsNoTracking() 
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
        {
            return NotFound();
        }

        return View(quiz);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitQuiz(int quizId, Dictionary<int, int> selectedAnswers)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var questionsInQuiz = await _context.Questions
            .Where(q => q.QuizId == quizId)
            .Include(q => q.Answers)
            .AsNoTracking()
            .ToListAsync();

        if (!questionsInQuiz.Any())
        {
            return BadRequest("Quiz nie ma pytań.");
        }

        
        int userPoints = 0;
        int maxPossiblePoints = 0;

        foreach (var question in questionsInQuiz)
        {
       
            maxPossiblePoints += question.Points;

            var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect);
            if (correctAnswer != null)
            {
                
                if (selectedAnswers.TryGetValue(question.Id, out int userAnswerId) && userAnswerId == correctAnswer.Id)
                {
                    
                    userPoints += question.Points;
                }
            }
        }

      
        int score = (maxPossiblePoints > 0)
            ? (int)Math.Round((double)userPoints / maxPossiblePoints * 100)
            : 0;

        var newQuizAttempt = new QuizAttempt
        {
            QuizId = quizId,
            UserId = userId,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow,
            Score = score 
        };

        foreach (var answerPair in selectedAnswers)
        {
            var attemptAnswer = new AttemptAnswer
            {
                SelectedAnswerId = answerPair.Value
            };
            newQuizAttempt.AttemptAnswers.Add(attemptAnswer);
        }

        _context.QuizAttempts.Add(newQuizAttempt);
        await _context.SaveChangesAsync();

        return RedirectToAction("AttemptDetails", "Statistics", new { id = newQuizAttempt.Id });
    }
}