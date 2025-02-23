using Mcg.Pcms.Core;
using Microsoft.EntityFrameworkCore;

namespace Mcg.Pcms.Infrastructure;

public class PatientDbContext(DbContextOptions<PatientDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Generate the ID when we add a patient rather than when we save the patient. This simplifies how we handle
        // the situation when we create a new patient and, somehow, find that they already exist in blob storage. It
        // shouldn't ever happen, but...
        modelBuilder.Entity<Patient>().Property(p => p.Id).ValueGeneratedOnAdd();
    }
}