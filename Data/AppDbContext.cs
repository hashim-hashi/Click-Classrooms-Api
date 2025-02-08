using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){

    }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Teacher> Teachers { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
}