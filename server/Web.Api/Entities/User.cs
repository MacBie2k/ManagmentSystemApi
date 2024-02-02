namespace Web.Api.Entities;

public class User : Entity<Guid>
{
   public string Email { get; set; }
   public string Password { get; set; }
   public string FullName { get; set; }
   public virtual List<UserProject> UserProjects { get; set;}
}