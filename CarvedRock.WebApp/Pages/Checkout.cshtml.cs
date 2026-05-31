using CarvedRock.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarvedRock.WebApp.Pages;

[Authorize]
[ValidateAntiForgeryToken]
public class CheckoutModel(ICartService cartService) : PageModel
{
    public string EmailAddress { get; set; } = "";

    public List<CartItemModel> CartContents { get; set; } = [];
    public double CartTotal => CartContents.Sum(c => c.Total);

    public async Task OnGetAsync()
    {
        EmailAddress = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "";
        CartContents = await cartService.GetCartAsync();
    }

    public async Task<IActionResult> OnPostSubmitOrder()
    {
        EmailAddress = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "";

        CartContents = await cartService.GetCartAsync();
        if (CartContents.Count == 0) return RedirectToPage("/Cart");

        // The API now owns order persistence and the confirmation email; the web app
        // just supplies the authenticated user's email (which the API token may lack).
        await cartService.PlaceOrderAsync(EmailAddress);

        return RedirectToPage("/ThankYou");
    }

    public async Task<IActionResult> OnPostCancelOrder()
    {
        await cartService.ClearCartAsync();
        return RedirectToPage("/Index");
    }
}
