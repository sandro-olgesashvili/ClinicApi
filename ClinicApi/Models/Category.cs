using System;
namespace ClinicApi.Models
{
	public class Category
	{
        public int Id { get; set; }

        public string CategoryName { get; set; }

        public List<User> Users { get; set; }
    }
}

