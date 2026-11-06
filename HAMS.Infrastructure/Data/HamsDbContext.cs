using System;
using Microsoft.EntityFrameworkCore;
using HAMS.Domain.Entities;

namespace HAMS.Infrastructure.Data
{
    public class HamsDbContext : DbContext
    {
        public HamsDbContext(DbContextOptions<HamsDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
        public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
        public DbSet<PrescriptionDetail> PrescriptionDetails => Set<PrescriptionDetail>();
        public DbSet<Medicine> Medicines => Set<Medicine>();
        public DbSet<MedicalService> MedicalServices => Set<MedicalService>();
        public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<VitalSigns> VitalSigns => Set<VitalSigns>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // RowVersion optimistic concurrency tokens
            modelBuilder.Entity<TimeSlot>()
                .Property(t => t.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<Appointment>()
                .Property(a => a.RowVersion)
                .IsRowVersion();

            // Decimal precisions
            modelBuilder.Entity<Doctor>()
                .Property(d => d.PricePerSlot)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Department>()
                .Property(d => d.BaseConsultationFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.DepositFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Medicine>()
                .Property(m => m.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PrescriptionDetail>()
                .Property(p => p.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<MedicalService>()
                .Property(s => s.BasePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ServiceOrder>()
                .Property(s => s.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<VitalSigns>()
                .Property(v => v.Temperature)
                .HasPrecision(4, 1);

            modelBuilder.Entity<VitalSigns>()
                .Property(v => v.Weight)
                .HasPrecision(5, 2);

            modelBuilder.Entity<VitalSigns>()
                .Property(v => v.Height)
                .HasPrecision(5, 2);

            // Prevent SQL Server cyclical cascade deletes
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // Relationships
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber);

            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Code)
                .IsUnique();

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.TimeSlot)
                .WithOne(t => t.Appointment)
                .HasForeignKey<Appointment>(a => a.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MedicalRecord>()
                .HasOne(m => m.Appointment)
                .WithOne(a => a.MedicalRecord)
                .HasForeignKey<MedicalRecord>(m => m.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VitalSigns>()
                .HasOne(v => v.Appointment)
                .WithOne(a => a.VitalSigns)
                .HasForeignKey<VitalSigns>(v => v.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Appointment)
                .WithOne(a => a.Feedback)
                .HasForeignKey<Feedback>(f => f.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Audit Log Relationships
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.CheckedInByReceptionist)
                .WithMany()
                .HasForeignKey(a => a.CheckedInByReceptionistId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PrescriptionDetail>()
                .HasOne(p => p.DispensedByPharmacist)
                .WithMany()
                .HasForeignKey(p => p.DispensedByPharmacistId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Cashier)
                .WithMany()
                .HasForeignKey(p => p.CashierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceOrder>()
                .HasOne(s => s.Technician)
                .WithMany()
                .HasForeignKey(s => s.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
