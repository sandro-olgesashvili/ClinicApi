using System;
namespace ClinicApi.Dto
{
	public class AppointmentAdminDto
	{
        public int Id { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int? PatientId { get; set; } = null;
    }
}

