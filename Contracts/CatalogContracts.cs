using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Equipo5.Api.Models;

namespace Equipo5.Api.Contracts;

public class ProductInput
{
    [Required, StringLength(40)] public string ProductName { get; set; } = "";
    [Range(1, int.MaxValue)] public int? SupplierId { get; set; }
    [Range(1, int.MaxValue)] public int? CategoryId { get; set; }
    [StringLength(20)] public string? QuantityPerUnit { get; set; }
    [Range(typeof(decimal), "0", "999999999999999.9999")] public decimal? UnitPrice { get; set; }
    [Range(0, short.MaxValue)] public short? UnitsInStock { get; set; }
    [Range(0, short.MaxValue)] public short? UnitsOnOrder { get; set; }
    [Range(0, short.MaxValue)] public short? ReorderLevel { get; set; }
    public bool Discontinued { get; set; }

    public void Apply(Product p)
    {
        p.ProductName = ProductName.Trim(); p.SupplierId = SupplierId; p.CategoryId = CategoryId;
        p.QuantityPerUnit = QuantityPerUnit; p.UnitPrice = UnitPrice; p.UnitsInStock = UnitsInStock;
        p.UnitsOnOrder = UnitsOnOrder; p.ReorderLevel = ReorderLevel; p.Discontinued = (sbyte)(Discontinued ? 1 : 0);
    }
}
public record ProductView(int ProductId, string ProductName, int? SupplierId, int? CategoryId,
    string? QuantityPerUnit, decimal? UnitPrice, short? UnitsInStock, short? UnitsOnOrder, short? ReorderLevel, bool Discontinued)
{
    public static readonly Expression<Func<Product, ProductView>> Select = p => new(p.ProductId,
        p.ProductName, p.SupplierId, p.CategoryId, p.QuantityPerUnit, p.UnitPrice, p.UnitsInStock,
        p.UnitsOnOrder, p.ReorderLevel, p.Discontinued != 0);
}
public record ProductCardDto(int ProductId, string ProductName, string? QuantityPerUnit,
    decimal? UnitPrice, short? UnitsInStock, string? CategoryName);

public record PagedProductsResponse(IReadOnlyList<ProductCardDto> Items, int Page, int PageSize,
    int TotalCount, bool HasMore);

public class CategoryInput
{
    [Required, StringLength(15)] public string CategoryName { get; set; } = "";
    [StringLength(10000)] public string? Description { get; set; }
    // JSON represents byte[] as base64. MaxLength limits the decoded bytes.
    [MaxLength(262144)] public byte[]? Picture { get; set; }
    public void Apply(Category c) { c.CategoryName = CategoryName.Trim(); c.Description = Description; c.Picture = Picture; }
}
public record CategoryView(int CategoryId, string CategoryName, string? Description, byte[]? Picture)
{
    public static readonly Expression<Func<Category, CategoryView>> Select = c => new(c.CategoryId, c.CategoryName, c.Description, c.Picture);
}
public class SupplierInput
{
    [Required, StringLength(40)] public string CompanyName { get; set; } = "";
    [StringLength(30)] public string? ContactName { get; set; }
    [StringLength(30)] public string? ContactTitle { get; set; }
    [StringLength(60)] public string? Address { get; set; }
    [StringLength(15)] public string? City { get; set; }
    [StringLength(15)] public string? Region { get; set; }
    [StringLength(10)] public string? PostalCode { get; set; }
    [StringLength(15)] public string? Country { get; set; }
    [StringLength(24)] public string? Phone { get; set; }
    [StringLength(24)] public string? Fax { get; set; }
    [StringLength(2000)] public string? HomePage { get; set; }
    public void Apply(Supplier s)
    {
        s.CompanyName = CompanyName.Trim(); s.ContactName = ContactName; s.ContactTitle = ContactTitle;
        s.Address = Address; s.City = City; s.Region = Region; s.PostalCode = PostalCode;
        s.Country = Country; s.Phone = Phone; s.Fax = Fax; s.HomePage = HomePage;
    }
}
public record SupplierView(int SupplierId, string CompanyName, string? ContactName, string? ContactTitle,
    string? Address, string? City, string? Region, string? PostalCode, string? Country, string? Phone, string? Fax, string? HomePage)
{
    public static readonly Expression<Func<Supplier, SupplierView>> Select = s => new(s.SupplierId,
        s.CompanyName, s.ContactName, s.ContactTitle, s.Address, s.City, s.Region, s.PostalCode, s.Country, s.Phone, s.Fax, s.HomePage);
}
