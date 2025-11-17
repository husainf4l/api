using Microsoft.AspNetCore.Mvc.RazorPages;
using AuthService.Repositories;
using AuthService.Models;

namespace AuthService.Pages.Applications;

public class IndexModel : PageModel
{
    private readonly IApplicationRepository _applicationRepository;

    public IndexModel(IApplicationRepository applicationRepository)
    {
        _applicationRepository = applicationRepository;
    }

    public List<Application> Applications { get; set; } = new();

    public async Task OnGetAsync()
    {
        Applications = await _applicationRepository.GetAllAsync();
    }
}
