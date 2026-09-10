namespace CommunitySportsBooking.Models
{
    public class Inquiry
    {
        public int InquiryID { get; set; }

        public string Name { get; set; } = "";

        public string Email { get; set; } = "";

        public string Subject { get; set; } = "";

        public string Message { get; set; } = "";

        public DateTime InquiryDate { get; set; }
    }
}
