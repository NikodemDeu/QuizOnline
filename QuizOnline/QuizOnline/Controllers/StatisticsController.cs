using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizOnline.Data;
using QuizOnline.Models;
using QuizOnline.ViewModels;

[Authorize]
public class StatisticsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public StatisticsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var userAttempts = await _context.QuizAttempts
            .Where(a => a.UserId == userId)
            .Include(a => a.Quiz)
            .OrderByDescending(a => a.EndTime)
            .ToListAsync();

    
        return View(userAttempts);
    }

    public async Task<IActionResult> AttemptDetails(int id)
    {
        var userId = _userManager.GetUserId(User);
        var attempt = await _context.QuizAttempts
            .Include(a => a.Quiz)
                .ThenInclude(q => q.Questions)
                    .ThenInclude(qt => qt.Answers)
            .Include(a => a.AttemptAnswers)
                .ThenInclude(aa => aa.SelectedAnswer)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attempt == null)
        {
            return NotFound();
        }

        if (attempt.UserId != userId)
        {
            return Forbid();
        }

        int userPoints = 0;
        int maxPossiblePoints = 0;

        foreach (var question in attempt.Quiz.Questions)
        {
            maxPossiblePoints += question.Points;

            var userAnswer = attempt.AttemptAnswers.FirstOrDefault(a => a.SelectedAnswer.QuestionId == question.Id);
            if (userAnswer != null && userAnswer.SelectedAnswer.IsCorrect)
            {
                userPoints += question.Points;
            }
        }

        var viewModel = new AttemptDetailsViewModel
        {
            Attempt = attempt,
            UserPoints = userPoints,
            MaxPossiblePoints = maxPossiblePoints
        };

        return View(viewModel); 
    }
}