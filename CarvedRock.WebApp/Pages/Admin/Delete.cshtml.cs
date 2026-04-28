using CarvedRock.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarvedRock.WebApp.Pages.Admin;

[Authorize(Roles = "admin")]
public class DeleteModel(IProductService productService) : PageModel
{
    [BindProperty]
    public ProductModel Product { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var product = await productService.GetProductByIdAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        Product = product;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        int id = Product.Id;
        
        try
        {
            bool deleted = await productService.DeleteProductAsync(id);
            
            if (!deleted)
            {
                return NotFound();
            }
            
            return RedirectToPage("./Index");
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "An error occurred while deleting the product.");
            
            // Reload the product data for the view
            var product = await productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            
            Product = product;
            return Page();
        }
    }
}