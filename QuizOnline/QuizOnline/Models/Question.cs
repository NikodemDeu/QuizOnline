using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuizOnline.Models;

public class Question
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Treść pytania jest wymagana.")]
    public string Text { get; set; } = string.Empty;

    [Display(Name = "Punkty")]
    [Range(1, 100, ErrorMessage = "Liczba punktów musi być w zakresie od 1 do 100.")]
    public int Points { get; set; } = 1; 

    public int QuizId { get; set; }

    [ForeignKey("QuizId")]
    public Quiz Quiz { get; set; } = null!; 

    
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}