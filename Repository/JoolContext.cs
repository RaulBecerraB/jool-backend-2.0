using System;
using Microsoft.EntityFrameworkCore;
using jool_backend.Models;
namespace jool_backend.Repository;

public class JoolContext : DbContext
{
    public JoolContext(DbContextOptions<JoolContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Hashtag> Hashtags { get; set; }
    public DbSet<Response> Responses { get; set; }
    public DbSet<QuestionHashtag> QuestionHashtags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de la clave primaria compuesta para QuestionHashtag
        modelBuilder.Entity<QuestionHashtag>()
            .HasKey(qh => new { qh.question_id, qh.hashtag_id });

        // Configuración de la relación entre Question y User
        modelBuilder.Entity<Question>()
            .HasOne(q => q.User)
            .WithMany(u => u.Questions)
            .HasForeignKey(q => q.user_id)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuración de la relación entre Response y User
        modelBuilder.Entity<Response>()
            .HasOne(r => r.User)
            .WithMany(u => u.Responses)
            .HasForeignKey(r => r.user_id)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuración de la relación entre Response y Question
        modelBuilder.Entity<Response>()
            .HasOne(r => r.Question)
            .WithMany(q => q.Responses)
            .HasForeignKey(r => r.question_id)
            .OnDelete(DeleteBehavior.NoAction);

        // Configuración de la relación entre QuestionHashtag y Question
        modelBuilder.Entity<QuestionHashtag>()
            .HasOne(qh => qh.Question)
            .WithMany(q => q.QuestionHashtags)
            .HasForeignKey(qh => qh.question_id)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuración de la relación entre QuestionHashtag y Hashtag
        modelBuilder.Entity<QuestionHashtag>()
            .HasOne(qh => qh.Hashtag)
            .WithMany(h => h.QuestionHashtags)
            .HasForeignKey(qh => qh.hashtag_id)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuración de índices únicos
        modelBuilder.Entity<User>()
            .HasIndex(u => u.email)
            .IsUnique();

        modelBuilder.Entity<Hashtag>()
            .HasIndex(h => h.name)
            .IsUnique();
    }
}
