using EduBook.API_New.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.SqlServer;
using System;
using System.Collections.Generic;
using System.Data;
namespace EduBook.API_New.Data
{
    public class AppDbContext : DbContext
    {
      

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }
      

        public DbSet<Material> Materials { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Roles> Rols { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Comment> Comments { get; set; }
       

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Roles>().HasData(
               new Roles { Role_Id = 1, Role = "Admin" },
               new Roles { Role_Id = 2, Role = "Author" },
               new Roles { Role_Id = 3, Role = "Student" }
              
           );

            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>()
                .Property(u => u.User_Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<User>()
                 .HasOne(u => u.Role)
                 .WithMany(r => r.Users)
                 .HasForeignKey(u => u.Role_id)
                 .OnDelete(DeleteBehavior.Restrict);

            // 2. Связь User -> Comments
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.User_id)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Связь Material -> Comments
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Material)
                .WithMany(m => m.CommentsList)
                .HasForeignKey(c => c.Material_id)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Связь Author -> Materials
            modelBuilder.Entity<Material>()
                .HasOne(m => m.Author)
                .WithMany(a => a.Materials)
                .HasForeignKey(m => m.Author_id)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. Связь User -> Author
            modelBuilder.Entity<Author>()
                .HasOne(a => a.User)
                .WithMany(u => u.Authors)
                .HasForeignKey(a => a.User_id)
                .OnDelete(DeleteBehavior.Restrict);
            // Настройка каскадного удаления: при удалении материала удаляются комментарии
            modelBuilder.Entity<Material>()
                .HasMany(m => m.CommentsList)
                .WithOne(c => c.Material)
                .HasForeignKey(c => c.Material_id)
                .OnDelete(DeleteBehavior.Cascade);  // Cascade - автоматическое удаление комментариев

            // При удалении пользователя - комментарии остаются (Restrict)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Comments)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.User_id)
                .OnDelete(DeleteBehavior.Restrict);


        }
    }
}
