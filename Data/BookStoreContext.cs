using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookStore.Models;

namespace BookStore.Data
{
    public class BookStoreContext : DbContext
    {
        public BookStoreContext (DbContextOptions<BookStoreContext> options)
            : base(options)
        {
        }

        public DbSet<BookStore.Models.Author> Author { get; set; } = default!;

        public DbSet<BookStore.Models.Book>? Book { get; set; }

        public DbSet<BookStore.Models.Genre>? Genre { get; set; }

        public DbSet<BookStore.Models.UserBooks>? UserBooks { get; set; }

        public DbSet<BookStore.Models.BookGenre>? BookGenre { get; set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BookGenre>()
                .HasOne<Genre>(p => p.Genre)
                .WithMany(p => p.Books)
                .HasForeignKey(p => p.GenreId)
                .HasPrincipalKey(p => p.Id);

            modelBuilder.Entity<BookGenre>()
                .HasOne<Book>(p => p.Book)
                .WithMany(p => p.Genres)
                .HasForeignKey(p => p.BookId)
                .HasPrincipalKey(p => p.Id);

            modelBuilder.Entity<Book>()
                .HasOne<Author>(p => p.Author)
                .WithMany(p => p.Books)
                .HasForeignKey(p => p.AuthorId)
                .HasPrincipalKey(p => p.Id);

            modelBuilder.Entity<Review>()
                .HasOne<Book>( p => p.Book)
                .WithMany(p => p.Reviews)
                .HasForeignKey(p => p.BookId)
                .HasPrincipalKey(p => p.Id);

            modelBuilder.Entity<UserBooks>()
                .HasOne<Book>(p => p.Book)
                .WithMany(p => p.UserBooks)
                .HasForeignKey(p => p.BookId)
                .HasPrincipalKey(p => p.Id);
           

        }

        public DbSet<BookStore.Models.Review>? Review { get; set; }
    }
}
