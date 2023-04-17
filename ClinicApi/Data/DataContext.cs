using System;
using ClinicApi.Models;

namespace ClinicApi.Data
{
	public class DataContext : DbContext
	{
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Appointment> Appointments { get; set; }
    }
}


