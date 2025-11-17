using Microsoft.AspNetCore.Mvc.RazorPages;
using AuthService.Repositories;
using AuthService.Models;

namespace AuthService.Pages.Users;

public class IndexModel : PageModel
{
    private readonly IUserRepository _userRepository;

    public IndexModel(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public List<User> Users { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Get users from database
        var user1 = await _userRepository.GetByEmailAsync("testuser@example.com");
        var user2 = await _userRepository.GetByEmailAsync("john@example.com");
        
        Users = new List<User>();
        if (user1 != null) Users.Add(user1);
        if (user2 != null) Users.Add(user2);
    }
}
