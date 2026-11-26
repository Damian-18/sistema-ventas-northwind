using System.ComponentModel.DataAnnotations;
using Equipo5.Api.Models;

namespace Equipo5.Api.Contracts;

public class CreateCustomerRequest
{
    [StringLength(5, MinimumLength = 5)]
    public string? CustomerId { get; set; }
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

    public void Apply(Customer customer)
    {
        customer.CompanyName = CompanyName;
        customer.ContactName = ContactName;
        customer.ContactTitle = ContactTitle;
        customer.Address = Address;
        customer.City = City;
        customer.Region = Region;
        customer.PostalCode = PostalCode;
        customer.Country = Country;
        customer.Phone = Phone;
        customer.Fax = Fax;
    }
}

public class UpdateCustomerRequest
{
    [StringLength(30)] public string? ContactName { get; set; }
    [StringLength(30)] public string? ContactTitle { get; set; }
    [StringLength(60)] public string? Address { get; set; }
    [StringLength(15)] public string? City { get; set; }
    [StringLength(15)] public string? Region { get; set; }
    [StringLength(10)] public string? PostalCode { get; set; }
    [StringLength(15)] public string? Country { get; set; }
    [StringLength(24)] public string? Phone { get; set; }
    [StringLength(24)] public string? Fax { get; set; }

    public void Apply(Customer customer)
    {
        customer.ContactName = ContactName;
        customer.ContactTitle = ContactTitle;
        customer.Address = Address;
        customer.City = City;
        customer.Region = Region;
        customer.PostalCode = PostalCode;
        customer.Country = Country;
        customer.Phone = Phone;
        customer.Fax = Fax;
    }
}

public record CustomerListDto(
    string CustomerId, string CompanyName, string? ContactName, string? City,
    string? Country, bool IsActive);

public record CustomerDetailDto(
    string CustomerId, string CompanyName, string? ContactName, string? ContactTitle,
    string? Address, string? City, string? Region, string? PostalCode, string? Country,
    string? Phone, string? Fax, bool IsActive);

public record PagedCustomersResponse(
    IReadOnlyList<CustomerListDto> Items, int Limit, int Offset, int TotalCount);
