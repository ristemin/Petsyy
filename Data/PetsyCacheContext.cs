using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Petsy.Models;
namespace Petsy.Data
{
    namespace Petsy.Data
    {
        public class PetsyDbContext : DbContext
        {
            public PetsyDbContext(DbContextOptions<PetsyDbContext> options)
                : base(options)
            {
            }

            public DbSet<Person> People { get; set; } = default!;
            public DbSet<Pet> Pets { get; set; } = default!;
            public DbSet<Vaccine> Vaccines { get; set; } = default!;
        }
    }
}