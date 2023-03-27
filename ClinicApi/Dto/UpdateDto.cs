using System;
namespace ClinicApi.Dto
{
	public class UpdateDto
	{
		public string Email { get; set; }

		public string Password { get; set; }

        public string ConfirmationToken { get; set; }

    }
}

