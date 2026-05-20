using System;
using Microsoft.EntityFrameworkCore;

namespace LocalServicesPlatform.Models;

public partial class LocalServicesDbContext : DbContext
{
    public LocalServicesDbContext()
    {
    }

    public LocalServicesDbContext(DbContextOptions<LocalServicesDbContext> options)
        : base(options)
    {
    }

    // This connects your C# code to your SQL Tables
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<ServiceCategory> ServiceCategories { get; set; }
    public virtual DbSet<Provider> Providers { get; set; }
    public virtual DbSet<Service> Services { get; set; }
    public virtual DbSet<ServiceImage> ServiceImages { get; set; }
    public virtual DbSet<Booking> Bookings { get; set; }
    public virtual DbSet<Review> Reviews { get; set; }
    public virtual DbSet<Message> Messages { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<Favorite> Favorites { get; set; }
    public virtual DbSet<Report> Reports { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {

        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // We use Data Annotations in DataModels.cs instead of putting logic here to keep it clean
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}