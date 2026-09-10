namespace CommunitySportsBooking.Models
{
    public class Booking
    {
        public int BookingID { get; set; }

        public int MemberID { get; set; }

        public int FacilityID { get; set; }

        public DateTime BookingDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string BookingStatus { get; set; } = "";

        public string FacilityName { get; set; } = "";

        public decimal Amount { get; set; }

        public int PaymentID { get; set; }
    }
}