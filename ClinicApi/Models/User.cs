using System;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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

        public string? ConfirmationToken { get; set; } = null;

        public string? ConfirmationTokenEmail { get; set; } = null;

        public string Role { get; set; } = "user";

        [Column(TypeName = "nvarchar(100)")]
        public string? ImageName { get; set; } = null;

        [NotMapped]
        public IFormFile? ImageFile { get; set; } = null;

        [NotMapped]
        public string? ImageSrc { get; set; } = null;


        [Column(TypeName = "nvarchar(100)")]
        public string? PdfName { get; set; } = null;

        [NotMapped]
        public IFormFile? PdfFile { get; set; } = null;

        [NotMapped]
        public string? PdfSrc { get; set; } = null;

        public DateTime EmailConfirmationTokenExpiration { get; set; }

        public bool IsPinned { get; set; } = false;

        public string? Description { get; set; } = null;

        public int? CategoryId { get; set; } = null;

        public Category Category { get; set; }

        [JsonIgnore]
        public List<Appointment> Appointments { get; set; }

    }
}

