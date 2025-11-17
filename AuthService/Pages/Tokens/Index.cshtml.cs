using Microsoft.AspNetCore.Mvc.RazorPages;
using AuthService.Repositories;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Pages.Tokens;

public class IndexModel : PageModel
{
    private readonly Data.AuthDbContext _context;

    public IndexModel(Data.AuthDbContext context)
    {
        _context = context;
    }

    public List<RefreshToken> Tokens { get; set; } = new();
    public int TotalTokens { get; set; }
    public int ActiveTokens { get; set; }
    public int ExpiredTokens { get; set; }
    public int RevokedTokens { get; set; }

    public async Task OnGetAsync()
    {
        Tokens = await _context.RefreshTokens
            .Include(t => t.User)
            .Include(t => t.Application)
            .OrderByDescending(t => t.CreatedAt)
            .Take(100)
            .ToListAsync();

        TotalTokens = Tokens.Count;
        ActiveTokens = Tokens.Count(t => t.IsActive);
        ExpiredTokens = Tokens.Count(t => t.IsExpired && !t.IsRevoked);
        RevokedTokens = Tokens.Count(t => t.IsRevoked);
    }
}
