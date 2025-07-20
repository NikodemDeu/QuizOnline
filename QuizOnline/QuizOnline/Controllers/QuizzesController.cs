using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuizOnline.Data;
using QuizOnline.Models;
using Microsoft.AspNetCore.Identity;
using QuizOnline.ViewModels;


namespace QuizOnline.Controllers
{
    public class QuizzesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public QuizzesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var userQuizzes = await _context.Quizzes
                .Where(q => q.UserId == userId)
                .Include(q => q.Questions)
                .AsNoTracking()
                .ToListAsync();

            return View(userQuizzes);
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }


            var currentUserId = _userManager.GetUserId(User);
            if (quiz.UserId != currentUserId)
            {
                return Forbid();
            }


            var answerStats = await _context.AttemptAnswers
                .Where(aa => aa.SelectedAnswer.Question.QuizId == id)
                .GroupBy(aa => aa.SelectedAnswerId)
                .Select(g => new { AnswerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.AnswerId, x => x.Count);

            var totalAttempts = await _context.QuizAttempts
                   .Where(a => a.QuizId == id)
                     .CountAsync();


            var averageScore = await _context.QuizAttempts
                .Where(a => a.QuizId == id)
                .AverageAsync(a => (double?)a.Score) ?? 0;


            var viewModel = new QuizDetailsViewModel
            {
                Quiz = quiz,
                AnswerSelectionStats = answerStats,
                TotalParticipants = totalAttempts,
                AverageScore = averageScore
            };


            foreach (var question in quiz.Questions)
            {
                int totalAnswersForThisQuestion = 0;
                foreach (var answer in question.Answers)
                {
                    if (answerStats.TryGetValue(answer.Id, out int count))
                    {
                        totalAnswersForThisQuestion += count;
                    }
                }
                viewModel.QuestionTotalAnswers[question.Id] = totalAnswersForThisQuestion;
            }

            return View(viewModel);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description")] Quiz quizz)
        {

            var userId = _userManager.GetUserId(User);
            if (userId == null) { return Challenge(); }
            quizz.UserId = userId;


            ModelState.Remove(nameof(Quiz.UserId));


            if (ModelState.IsValid)
            {
                _context.Add(quizz);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(quizz);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quizz = await _context.Quizzes.FindAsync(id);
            if (quizz == null)
            {
                return NotFound();
            }
            return View(quizz);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description")] Quiz quiz)
        {
            if (id != quiz.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(quiz);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuizzExists(quiz.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(quiz);
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quizz = await _context.Quizzes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (quizz == null)
            {
                return NotFound();
            }

            return View(quizz);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var quizz = await _context.Quizzes.FindAsync(id);
            if (quizz != null)
            {
                _context.Quizzes.Remove(quizz);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool QuizzExists(int id)
        {
            return _context.Quizzes.Any(e => e.Id == id);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(Question question)
        {

            ModelState.Remove(nameof(question.Quiz));
            if (ModelState.IsValid)
            {
                _context.Questions.Add(question);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = question.QuizId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAnswer([Bind("QuestionId,Text,IsCorrect")] Answer answer)
        {
            ModelState.Remove(nameof(answer.Question));
            if (ModelState.IsValid)
            {
                _context.Answers.Add(answer);
                await _context.SaveChangesAsync();
            }

            var question = await _context.Questions.FindAsync(answer.QuestionId);
            if (question == null)
            {

                return RedirectToAction("Index", "Home");
            }


            return RedirectToAction(nameof(Details), new { id = question.QuizId });
        }
    }
}
