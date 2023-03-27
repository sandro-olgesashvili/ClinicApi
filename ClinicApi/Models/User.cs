using System;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicApi.Models
{
	public class User
	{
        public int Id { get; set; }

        public string Name { get; set; }

        public string Surname { get; set; }

        public string IdNumber { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public bool IsConfirmed { get; set; }

        public string ConfirmationToken { get; set; }

        public string Role { get; set; } = "user";

        //[Column(TypeName = "nvarchar(100)")]
        //public string ImageName { get; set; }

        //[NotMapped]
        //public IFormFile ImageFile { get; set; }

        //[NotMapped]
        //public string ImageSrc { get; set; }

        public DateTime EmailConfirmationTokenExpiration { get; set; }

    }
}

