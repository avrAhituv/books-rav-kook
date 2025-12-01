using Microsoft.EntityFrameworkCore;
using HolyBooks.Core.Models;

namespace HolyBooks.Data.Database;

public class AppDbContext : DbContext
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Content> Contents => Set<Content>();

    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<BookmarkFolder> BookmarkFolders => Set<BookmarkFolder>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<NoteTag> NoteTags => Set<NoteTag>();
    public DbSet<ReadingHistory> ReadingHistory => Set<ReadingHistory>();
    public DbSet<ReadingSession> ReadingSessions => Set<ReadingSession>();
    public DbSet<TextLink> TextLinks => Set<TextLink>();
    public DbSet<ExternalReference> ExternalReferences => Set<ExternalReference>();

    public DbSet<SourceSheet> SourceSheets => Set<SourceSheet>();
    public DbSet<SheetFolder> SheetFolders => Set<SheetFolder>();
    public DbSet<SheetItem> SheetItems => Set<SheetItem>();
    public DbSet<SheetTag> SheetTags => Set<SheetTag>();

    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<SefariaCache> SefariaCache => Set<SefariaCache>();

    private readonly string _dbPath;

    public AppDbContext()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "HolyBooks");
        Directory.CreateDirectory(appFolder);
        _dbPath = Path.Combine(appFolder, "holybooks.db");
    }

    public AppDbContext(string dbPath)
    {
        _dbPath = dbPath;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Book
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasIndex(e => e.Title);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.SefariaRef).IsUnique();
        });

        // Chapter - Self-referencing hierarchy
        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Book)
                .WithMany(b => b.Chapters)
                .HasForeignKey(c => c.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.BookId, e.SortOrder });
        });

        // Content
        modelBuilder.Entity<Content>(entity =>
        {
            entity.HasOne(c => c.Chapter)
                .WithOne(ch => ch.Content)
                .HasForeignKey<Content>(c => c.ChapterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BookmarkFolder - Self-referencing
        modelBuilder.Entity<BookmarkFolder>(entity =>
        {
            entity.HasOne(f => f.Parent)
                .WithMany(f => f.Children)
                .HasForeignKey(f => f.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Bookmark
        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.HasOne(b => b.Content)
                .WithMany(c => c.Bookmarks)
                .HasForeignKey(b => b.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(b => b.Folder)
                .WithMany(f => f.Bookmarks)
                .HasForeignKey(b => b.FolderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Note
        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasOne(n => n.Content)
                .WithMany(c => c.Notes)
                .HasForeignKey(n => n.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ContentId);
        });

        // NoteTag
        modelBuilder.Entity<NoteTag>(entity =>
        {
            entity.HasOne(t => t.Note)
                .WithMany(n => n.Tags)
                .HasForeignKey(t => t.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Tag);
        });

        // TextLink
        modelBuilder.Entity<TextLink>(entity =>
        {
            entity.HasOne(l => l.SourceContent)
                .WithMany(c => c.SourceLinks)
                .HasForeignKey(l => l.SourceContentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.TargetContent)
                .WithMany(c => c.TargetLinks)
                .HasForeignKey(l => l.TargetContentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ReadingHistory
        modelBuilder.Entity<ReadingHistory>(entity =>
        {
            entity.HasIndex(e => e.LastRead);
            entity.HasIndex(e => e.ContentId).IsUnique();
        });

        // SheetFolder - Self-referencing
        modelBuilder.Entity<SheetFolder>(entity =>
        {
            entity.HasOne(f => f.Parent)
                .WithMany(f => f.Children)
                .HasForeignKey(f => f.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SourceSheet
        modelBuilder.Entity<SourceSheet>(entity =>
        {
            entity.HasOne(s => s.Folder)
                .WithMany(f => f.Sheets)
                .HasForeignKey(s => s.FolderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SheetItem
        modelBuilder.Entity<SheetItem>(entity =>
        {
            entity.HasOne(i => i.Sheet)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.SheetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.SheetId, e.SortOrder });
        });

        // SheetTag
        modelBuilder.Entity<SheetTag>(entity =>
        {
            entity.HasOne(t => t.Sheet)
                .WithMany(s => s.Tags)
                .HasForeignKey(t => t.SheetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Tag);
        });

        // AppSettings
        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // SefariaCache
        modelBuilder.Entity<SefariaCache>(entity =>
        {
            entity.HasIndex(e => e.Ref).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
        });

        // ExternalReference
        modelBuilder.Entity<ExternalReference>(entity =>
        {
            entity.HasIndex(e => new { e.RefType, e.RefBook });
        });
    }
}
