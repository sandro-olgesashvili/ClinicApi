using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ClinicApi.Interfaces;
using ClinicApi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClinicApi.Service
{
	public class TokenService : ITokenService
	{
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;

        }
        public string GenerateToken(User req)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, req.Email),
                new Claim(ClaimTypes.Role, req.Role),
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}

