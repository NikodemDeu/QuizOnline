using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuizOnline.Models;

public class Answer
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Treść odpowiedzi jest wymagana.")]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    
    public int QuestionId { get; set; }

    [ForeignKey("QuestionId")]
    public Question Question { get; set; } = null!;
}