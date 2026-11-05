using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this WebApplication app)
    {
        var categories = app.MapGroup("/api/categories").RequireAuthorization();

        categories.MapGet("/", (AppDb db) => db.Categories.OrderBy(c => c.Name)
            .Select(c => new NameCount(c.Id, c.Name, db.Events.Count(e => e.CategoryId == c.Id)))
            .ToListAsync()).WithName("ListCategories");

        categories.MapPost("/", async (NameIn body, AppDb db) =>
        {
            var name = body.Name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 100) return Failure.Invalid("name", "Name is required (max 100).");
            if (await db.Categories.AnyAsync(c => c.Name == name)) return Results.Conflict(new { error = "Category already exists." });
            var c = new Category { Name = name };
            db.Categories.Add(c);
            await db.SaveChangesAsync();
            return Results.Created($"/api/categories/{c.Id}", c);
        }).RequireAuthorization("Editor").WithName("CreateCategory").Produces<Category>(201);

        categories.MapPut("/{id:int}", async (int id, NameIn body, AppDb db) =>
        {
            var name = body.Name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 100) return Failure.Invalid("name", "Name is required (max 100).");
            var c = await db.Categories.FindAsync(id);
            if (c is null) return Results.NotFound();
            if (await db.Categories.AnyAsync(x => x.Name == name && x.Id != id)) return Results.Conflict(new { error = "Category already exists." });
            c.Name = name;
            await db.SaveChangesAsync();
            return Results.Ok(c);
        }).RequireAuthorization("Editor").WithName("UpdateCategory").Produces<Category>();

        categories.MapDelete("/{id:int}", async (int id, AppDb db) =>
        {
            var c = await db.Categories.FindAsync(id);
            if (c is null) return Results.NotFound();
            if (await db.Events.AnyAsync(e => e.CategoryId == id)) return Results.Conflict(new { error = "Category is used by events." });
            db.Categories.Remove(c);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("Editor").WithName("DeleteCategory");
    }
}
