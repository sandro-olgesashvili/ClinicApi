using System;
namespace ClinicApi.Models
{
	public class Appointment
	{
        public int Id { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int? PatientId { get; set; } = null;

        public int UserId { get; set; }

        public User User { get; set; }
    }
}

