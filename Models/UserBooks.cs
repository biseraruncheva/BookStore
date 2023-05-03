using System.ComponentModel.DataAnnotations;

namespace BookStore.Models
{
    public class UserBooks
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string AppUser { get; set; }

        [Display(Name = "Book")]
        public int BookId { get; set; }
        public Book? Book { get; set; }
    }
}
