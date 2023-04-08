using System;
using System.Text.Json.Serialization;

namespace ClinicApi.Models
{
	public class Category
	{
        public int Id { get; set; }

        public string CategoryName { get; set; }

        [JsonIgnore]
        public List<User> Users { get; set; }
    }
}

