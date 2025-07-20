using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace QuizOnline.Models;

public class QuizAttempt
{
    [Key]
    public int Id { get; set; }

   
    public int QuizId { get; set; }

    [ForeignKey("QuizId")]
    public Quiz Quiz { get; set; } = null!;
  
    [Required]
    public string UserId { get; set; } = string.Empty;

   

    [Required]
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; } 

    [Range(0, 100, ErrorMessage = "Wynik musi być w zakresie od 0 do 100.")]
    public int Score { get; set; } 

    
    public ICollection<AttemptAnswer> AttemptAnswers { get; set; } = new List<AttemptAnswer>();
}