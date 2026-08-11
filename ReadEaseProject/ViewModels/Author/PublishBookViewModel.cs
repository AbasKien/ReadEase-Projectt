using System.ComponentModel.DataAnnotations;

namespace ReadEase.ViewModels.Author
{
    public class PublishBookViewModel
    {
        public Guid? Id { get; set; } // null = create, set = edit

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title max 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required.")]
        [StringLength(500, ErrorMessage = "Categories max 500 characters.")]
        public string Category { get; set; } = string.Empty;

        [StringLength(10000, ErrorMessage = "Description max 10000 characters.")]
        public string? Description { get; set; }

        public bool IsFree { get; set; } = true;

        public IFormFile? CoverImageFile { get; set; }
        public string? ExistingCoverImage { get; set; }
    }
}
