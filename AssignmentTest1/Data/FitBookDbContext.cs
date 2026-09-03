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
        public DbSet<Payment> Payments { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<MembershipPlan> MembershipPlans { get; set; }
        public DbSet<MemberSubscription> MemberSubscriptions { get; set; }
        public DbSet<FitnessClass> FitnessClasses { get; set; }
        public DbSet<ClassSchedule> ClassSchedules { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Waitlist> Waitlists { get; set; }
        public DbSet<TrainerPhoto> TrainerPhotos { get; set; }

        public DbSet<PrivateSession> PrivateSessions { get; set; }
        public DbSet<ClassScheduleTemplate> ClassScheduleTemplates { get; set; }
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

            // ✅ Payment → User
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ Payment → Subscription (Optional)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Subscription)
                .WithOne(s => s.Payment)
                .HasForeignKey<Payment>(p => p.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MembershipPlan>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);  // 18位总长度，2位小数

            // Payment 的 Amount
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            // PrivateSession → Member (User)
            modelBuilder.Entity<PrivateSession>()
                .HasOne(ps => ps.Member)
                .WithMany(u => u.PrivateSessions)
                .HasForeignKey(ps => ps.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // PrivateSession → Trainer (User)
            modelBuilder.Entity<PrivateSession>()
                .HasOne(ps => ps.Trainer)
                .WithMany(u => u.TrainerSessions)
                .HasForeignKey(ps => ps.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            // PrivateSession → Subscription
            modelBuilder.Entity<PrivateSession>()
                .HasOne(ps => ps.Subscription)
                .WithMany(s => s.PrivateSessions)
                .HasForeignKey(ps => ps.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

        }

    }
}