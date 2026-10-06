using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.Data;
using OrderService.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

var optionsBuilder = new DbContextOptionsBuilder<OrderServiceDbContext>();
optionsBuilder.UseSqlServer("Server=localhost;Database=OrderServiceDb;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False");
optionsBuilder.EnableSensitiveDataLogging();
optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);

using var context = new OrderServiceDbContext(optionsBuilder.Options);

var userId = Guid.Parse("9f17ba07-dd3c-443c-8c16-cf7c715a2502");

var cart = await context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);

if (cart != null)
{
    Console.WriteLine($"Found cart: {cart.Id}. Updating...");
    cart.UpdatedAt = DateTime.UtcNow;
    cart.Items.Add(new CartItem
    {
        Id = Guid.NewGuid(), // THIS IS THE BUG
        CartId = cart.Id,
        ProductId = Guid.NewGuid(),
        Quantity = 1,
        CreatedAt = DateTime.UtcNow
    });
    
    context.Carts.Update(cart); // THIS IS WHAT THE REPOSITORY DOES!
    
    foreach (var entry in context.ChangeTracker.Entries())
    {
        Console.WriteLine($"ENTITY: {entry.Entity.GetType().Name} STATE: {entry.State}");
    }
    
    try
    {
        await context.SaveChangesAsync();
        Console.WriteLine("Saved successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception: {ex.GetType().Name} - {ex.Message}");
    }
}
else
{
    Console.WriteLine("Cart not found!");
}
