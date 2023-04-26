using System;
namespace ClinicApi.Dto
{
	public class PasswrodRestoreDto
	{
        public string Email { get; set; }

        public string ConfirmationToken { get; set; }
    }
}

