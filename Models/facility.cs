namespace CommunitySportsBooking.Models
{
    public class Facility
    {
        public int FacilityID { get; set; }

        public string FacilityName { get; set; } = "";

        public string FacilityType { get; set; } = "";

        public string Location { get; set; } = "";

        public string Description { get; set; } = "";

        public int Capacity { get; set; }

        public string Status { get; set; } = "";
    }
}