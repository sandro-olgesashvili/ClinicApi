using System;
using ClinicApi.Models;
namespace ClinicApi.Interfaces
{
	public interface ITokenService
	{
        string GenerateToken(User req);
    }
}

