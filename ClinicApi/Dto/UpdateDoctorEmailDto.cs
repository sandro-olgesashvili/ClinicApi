using System;
namespace ClinicApi.Dto
{
	public class UpdateDoctorEmailDto
	{
		public int Id { get; set; }

		public string Email { get; set; }

		public string? Name { get; set; }

	}
}

