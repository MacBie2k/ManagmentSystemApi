using Microsoft.EntityFrameworkCore;
using Web.Api.Entities;
using Task = System.Threading.Tasks.Task;

namespace Web.Api.Database;

public class ApplicationDBContext : DbContext
{
    public ApplicationDBContext(DbContextOptions options) : base(options) 
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .UseIdentityColumn();
        });
        
        
        modelBuilder.Entity<UserProject>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .UseIdentityColumn();
            entity.HasIndex(x => x.UserId);
            entity.HasOne(x => x.User)
                .WithMany(x => x.UserProjects)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.ProjectId);
            entity.HasOne(x => x.Project)
                .WithMany(x => x.UserProjects)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .UseIdentityColumn();
            entity.HasIndex(x => x.ProjectId);
            entity.HasOne(x => x.Project)
                .WithMany(x => x.ProjectTasks)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.UserProjectId);
            entity.HasOne(x => x.UserProject)
                .WithMany(x => x.ProjectTasks)
                .HasForeignKey(x => x.UserProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.Property(x => x.TaskStatus)
                .HasDefaultValue(TaskStatusEnum.Backlog);
        });
        
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .UseIdentityColumn();
            
            entity.HasIndex(x => x.ProjectTaskId);
            entity.HasOne(x => x.ProjectTask)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.ProjectTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.UserProjectId);
            entity.HasOne(x => x.UserProject)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.UserProjectId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<UserProject> UserProjects { get; set; }
    
    public DbSet<ProjectTask> ProjectTasks { get; set; }
    public DbSet<Comment> Comments { get; set; }


    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Entity && e.State is EntityState.Added or EntityState.Modified);

        foreach (var entityEntry in entries)
        {
            ((Entity)entityEntry.Entity).UpdatedTime = DateTime.Now;
            entityEntry.Property("UpdatedTime").IsModified = true;

            if (entityEntry.State == EntityState.Added)
            {
                ((Entity)entityEntry.Entity).CreatedTime = DateTime.Now;
            }
            else
            {
                entityEntry.Property("CreatedTime").IsModified = false;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}