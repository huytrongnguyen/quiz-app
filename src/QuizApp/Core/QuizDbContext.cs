using Microsoft.EntityFrameworkCore;

namespace QuizApp.Core;

public class QuizDbContext(DbContextOptions<QuizDbContext> options) : DbContext(options) {
  public DbSet<QuizQuestion> QuizQuestion { get; set; }
}