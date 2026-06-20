using Microsoft.EntityFrameworkCore;
using AssignmentTest1.Models.Entities;

namespace AssignmentTest1.Data
{
    public class FitBookDbContext : DbContext
    {
        public FitBookDbContext()
        {
        }
        public FitBookDbContext(DbContextOptions<FitBookDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\\FitBook.mdf;Integrated Security=True;Connect Timeout=30;");
            }
        }

        public DbSet<User> Users { get; set; }
        public DbSet<MembershipPlan> MembershipPlans { get; set; }
        public DbSet<MemberSubscription> MemberSubscriptions { get; set; }
        public DbSet<FitnessClass> FitnessClasses { get; set; }
        public DbSet<ClassSchedule> ClassSchedules { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Waitlist> Waitlists { get; set; }
        public DbSet<TrainerPhoto> TrainerPhotos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships with DELETE RESTRICT to avoid cascade issues

            // Booking → Member (User)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Member)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking → Schedule (ClassSchedule)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Schedule)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ClassSchedule → Class (FitnessClass)
            modelBuilder.Entity<ClassSchedule>()
                .HasOne(cs => cs.Class)
                .WithMany(fc => fc.Schedules)
                .HasForeignKey(cs => cs.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // Waitlist → Member (User)
            modelBuilder.Entity<Waitlist>()
                .HasOne(w => w.Member)
                .WithMany(u => u.Waitlists)
                .HasForeignKey(w => w.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // Waitlist → Schedule (ClassSchedule)
            modelBuilder.Entity<Waitlist>()
                .HasOne(w => w.Schedule)
                .WithMany(cs => cs.Waitlists)
                .HasForeignKey(w => w.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            // TrainerPhoto → Trainer (User)
            modelBuilder.Entity<TrainerPhoto>()
                .HasOne(tp => tp.Trainer)
                .WithMany(u => u.TrainerPhotos)
                .HasForeignKey(tp => tp.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            // MemberSubscription → User
            modelBuilder.Entity<MemberSubscription>()
                .HasOne(ms => ms.User)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(ms => ms.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // MemberSubscription → MembershipPlan
            modelBuilder.Entity<MemberSubscription>()
                .HasOne(ms => ms.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(ms => ms.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // FitnessClass → Trainer (User)
            modelBuilder.Entity<FitnessClass>()
                .HasOne(fc => fc.Trainer)
                .WithMany(u => u.Classes)
                .HasForeignKey(fc => fc.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);
        }

    }
}