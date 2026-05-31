using CarvedRock.Core;
using CarvedRock.Data;
using FluentValidation;

namespace CarvedRock.Domain;

public class AddToCartValidator : AbstractValidator<AddToCartModel>
{
    private readonly ICarvedRockRepository _repo;

    public AddToCartValidator(ICarvedRockRepository repo)
    {
        _repo = repo;

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.ProductId)
            .MustAsync(ProductExists).WithMessage("Product does not exist.");
    }

    private async Task<bool> ProductExists(int productId, CancellationToken token)
    {
        return await _repo.GetProductByIdAsync(productId) != null;
    }
}
