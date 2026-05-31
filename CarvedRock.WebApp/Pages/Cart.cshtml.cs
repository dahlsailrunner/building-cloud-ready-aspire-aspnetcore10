using CarvedRock.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarvedRock.WebApp.Pages;

[ValidateAntiForgeryToken]
public class CartModel(ICartService cartService) : PageModel
{
    public List<CartItemModel> CartContents { get; set; } = [];
    public double CartTotal => CartContents.Sum(c => c.Total);

    public async Task OnGetAsync()
    {
        CartContents = await cartService.GetCartAsync();
    }

    public IActionResult OnPostCheckout()
    {
        return RedirectToPage("/Checkout");
    }

    public async Task<IActionResult> OnPostCancelOrder()
    {
        await cartService.ClearCartAsync();
        return RedirectToPage("/Index");
    }
}
