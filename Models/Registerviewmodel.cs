using System.ComponentModel.DataAnnotations;

namespace CommunitySportsBooking.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Phone number is required.")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = "";

        public List<Sport> Sports { get; set; } = new List<Sport>();

        public List<int> SelectedSports { get; set; } = new List<int>();
    }
}
