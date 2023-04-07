using System;
namespace ClinicApi.Dto
{
	public class UpdateDoctorEmailConfirmDto
	{
		public int Id { get; set; }

		public string Email { get; set; }

        public string ConfirmationToken { get; set; }

        public string ConfirmationTokenEmail { get; set; }
    }
}

