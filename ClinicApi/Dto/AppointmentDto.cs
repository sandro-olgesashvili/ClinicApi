using System;
namespace ClinicApi.Dto
{
	public class AppointmentDto
    {
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int? PatientId { get; set; } = null;
    }
}

