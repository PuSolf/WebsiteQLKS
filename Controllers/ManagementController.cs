using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebsiteQLKS.Controllers;

[Authorize(Roles = "Manager,Receptionist")]
public class ManagementController : Controller
{
    [Authorize(Roles = "Manager")]
    public IActionResult Index() => View();

    public IActionResult Reception() => View();
}
