using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BookStore.Data;
using BookStore.Models;
using BookStore.ViewModels;
using BookStore.Interfaces;
using BookStore.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace BookStore.Controllers
{
    public class BooksController : Controller
    {
        private readonly BookStoreContext _context;
        readonly IBufferedFileUploadService _bufferedFileUploadService;
        readonly IStreamFileUploadService _streamFileUploadService;


        public BooksController(BookStoreContext context, IStreamFileUploadService streamFileUploadService, IBufferedFileUploadService bufferedFileUploadService)
        {
            _context = context;
            _streamFileUploadService = streamFileUploadService;
            _bufferedFileUploadService = bufferedFileUploadService;
        }


        // GET: Books
        public async Task<IActionResult> Index(string bookGenre, string searchString)
        {
            IQueryable<Book> books = _context.Book.AsQueryable();
            IQueryable<string> genreQuery = _context.Genre.OrderBy(b => b.Id).Select(b => b.GenreName);

            if (!string.IsNullOrEmpty(searchString))
            {
                books = books.Where(s => s.Title.Contains(searchString));
            }
            if (!string.IsNullOrEmpty(bookGenre))
            {

                books = books.Where(b => b.Genres.Any(bg => bg.Genre.GenreName == bookGenre));
            }

            books = books.Include(b => b.Author);

            var averageRatings = await _context.Review
                .GroupBy(r => r.BookId)
                .Select(g => new {
                    BookId = g.Key,
                    AverageRating = g.Average(r => r.Rating)
                })
                .ToDictionaryAsync(x => x.BookId, x => x.AverageRating);

            ViewBag.AverageRatings = averageRatings;


            var bookGenreVM = new BookGenreViewModel
            {
                Genres = new SelectList(await genreQuery.ToListAsync()),
                Books = await books.ToListAsync()
            };
            return View(bookGenreVM);
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Book == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .Include(b => b.Author)
                .Include(b => b.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            var averageRating = await _context.Review
                .Where(r => r.BookId == book.Id)
                .AverageAsync(r => r.Rating);

            ViewBag.AverageRating = averageRating;

            return View(book);
        }

        // GET: Books/Create
        public IActionResult Create()
        {
            //ViewData["GenreId"] = new MultiSelectList(_context.Genre, "Id", "GenreName");
            var genres = _context.Genre.Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.GenreName
            }).ToList();

            ViewData["Genres"] = genres;
            ViewData["AuthorId"] = new SelectList(_context.Author, "Id", "FullName");
            return View();
        }

        // POST: Books/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormFile file, IFormFile pdffile, [Bind("Id,Title,YearPublished,NumPages,Description,Publisher,FrontPage,DownloadUrl,AuthorId")] Book book, int[] selectedGenres)
        {
            ModelState.Remove("file");
            ModelState.Remove("pdffile");
            if (ModelState.IsValid)
            {
                try
                {
                    book.FrontPage = await _bufferedFileUploadService.UploadFile(file);
                    book.DownloadUrl = await _bufferedFileUploadService.UploadFile(pdffile);
                    if (!string.IsNullOrEmpty(book.FrontPage) && !string.IsNullOrEmpty(book.DownloadUrl))
                    {
                        ViewBag.Message = "File Upload Successful";
                    }
                    else
                    {
                        ViewBag.Message = "File Upload Failed";
                    }
                }
                catch (Exception ex)
                {
                    //Log ex
                    ViewBag.Message = "File Upload Failed";
                }
                _context.Add(book);
                await _context.SaveChangesAsync();
                if (selectedGenres != null)
                {
                    foreach (var genreId in selectedGenres)
                    {
                        var genreBook = new BookGenre
                        {
                            GenreId = genreId,
                            BookId = book.Id
                        };

                        _context.Add(genreBook);
                    }

                    await _context.SaveChangesAsync();
                }


                return RedirectToAction(nameof(Index));
            }



            ViewData["AuthorId"] = new SelectList(_context.Author, "Id", "FullName", book.AuthorId);
            return View(book);
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Book == null)
            {
                return NotFound();
            }

            var book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            var bookGenres = _context.BookGenre
                .Where(bg => bg.BookId == book.Id)
                .Select(bg => bg.GenreId)
                .ToList();

            var genres = _context.Genre.Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.GenreName,
                Selected = bookGenres.Contains(g.Id) // mark the genre as selected if the book has it
            }).ToList();

            ViewData["Genres"] = genres;
            ViewData["AuthorId"] = new SelectList(_context.Author, "Id", "FullName", book.AuthorId);
            return View(book);
        }


        // POST: Books/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IFormFile fileimg, IFormFile filepdf, [Bind("Id,Title,YearPublished,NumPages,Description,Publisher,FrontPage,DownloadUrl,AuthorId")] Book book, int[] selectedGenres)
        {

            if (id != book.Id)
            {
                return NotFound();
            }
            ModelState.Remove("filepdf");
            ModelState.Remove("fileimg");
            if (ModelState.IsValid)
            {
                var oldBook = await _context.Book.FindAsync(book.Id);
                if (fileimg?.Length > 0)
                {
                    try
                    {
                        book.FrontPage = await _bufferedFileUploadService.UploadFile(fileimg);
                        if (!string.IsNullOrEmpty(book.FrontPage))
                        {
                            ViewBag.Message = "File Upload Successful";
                        }
                        else
                        {
                            ViewBag.Message = "File Upload Failed";
                        }
                    }
                    catch (Exception ex)
                    {
                        //Log ex
                        ViewBag.Message = "File Upload Failed";
                    }


                }
                else
                {
                    book.FrontPage = oldBook.FrontPage;

                }

                if (filepdf?.Length > 0)
                {
                    try
                    {
                        book.DownloadUrl = await _bufferedFileUploadService.UploadFile(filepdf);
                        if (!string.IsNullOrEmpty(book.DownloadUrl))
                        {
                            ViewBag.Message = "File Upload Successful";
                        }
                        else
                        {
                            ViewBag.Message = "File Upload Failed";
                        }
                    }
                    catch (Exception ex)
                    {
                        //Log ex
                        ViewBag.Message = "File Upload Failed";
                    }
                }


                else
                {
                    book.DownloadUrl = oldBook.DownloadUrl;

                }

                try
                {
                    _context.Entry(oldBook).State = EntityState.Detached;
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                    if (selectedGenres != null)
                    {
                        // Remove old BookGenre records for the current book
                        var oldBookGenres = _context.BookGenre.Where(bg => bg.BookId == book.Id);
                        _context.BookGenre.RemoveRange(oldBookGenres);

                        // Add new BookGenre records for the current book
                        foreach (var genreId in selectedGenres)
                        {
                            var genreBook = new BookGenre
                            {
                                GenreId = genreId,
                                BookId = book.Id
                            };

                            _context.Update(genreBook);
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Author, "Id", "FullName", book.AuthorId);
            return View(book);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Book == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .Include(b => b.Author)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Book == null)
            {
                return Problem("Entity set 'BookStoreContext.Book'  is null.");
            }
            var book = await _context.Book.FindAsync(id);
            if (book != null)
            {
                _context.Book.Remove(book);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
          return (_context.Book?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
