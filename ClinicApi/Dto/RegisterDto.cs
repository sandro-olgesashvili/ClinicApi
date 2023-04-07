﻿using System;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicApi.Dto
{
	public class RegisterDto
	{
		public string Name { get; set; }

		public string Surname { get; set; }

		public string Email { get; set; }

		public string IdNumber { get; set; }

		public string Password { get; set; }

        public string? Role { get; set; } = "user";

        public int? CategoryId { get; set; } = null;

        //[Column(TypeName = "nvarchar(100)")]
        //public string ImageName { get; set; }

        //[NotMapped]
        //public IFormFile ImageFile { get; set; }

        //[NotMapped]
        //public string ImageSrc { get; set; }

    }
}

