using Microsoft.EntityFrameworkCore;

namespace REST_API
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<Node> Nodes { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Name { get; set; }

        public string Surname { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public int Manager { get; set; } = -1; // default hodnota aby vedel program rozlíšiť medzi 0 (ako false) a nulovou (nedodanou) hodnotou

        public string Code { get; set; }
    }

    public class Node
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public string ParentCode { get; set; }
    }
}