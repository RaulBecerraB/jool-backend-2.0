using System;
using Microsoft.EntityFrameworkCore;
using jool_backend.Models;
namespace jool_backend.Repository;

public class JoolContext : DbContext
{
    public JoolContext(DbContextOptions<JoolContext> options) : base(options)
    {
    }

    public DbSet<Hashtag> Hashtags { get; set; }
}
