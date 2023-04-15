using System;
namespace ClinicApi.Dto
{
	public class DoctorDto
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public string Surname { get; set; }

		public string Email { get; set; }

		public string IdNumber { get; set; }

		public string CategoryName { get; set; }

		public string Role { get; set; }

		public bool IsPinned { get; set; }

	}
}


