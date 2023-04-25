using System;
namespace ClinicApi.Dto
{
	public class LoginTwoFactorDto
	{
        public string Email { get; set; }

        public string Password { get; set; }

        public string TwoFactorStr { get; set; }

    }
}

