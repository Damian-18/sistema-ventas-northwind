using System.ComponentModel.DataAnnotations;
using Equipo5.Api.Models;

namespace Equipo5.Api.Contracts;

public class CreateEmployeeRequest
{
    [Required, StringLength(10)] public string FirstName { get; set; } = "";
    [Required, StringLength(20)] public string LastName { get; set; } = "";
    [StringLength(30)] public string? Title { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime? HireDate { get; set; }
    [StringLength(60)] public string? Address { get; set; }
    [StringLength(15)] public string? City { get; set; }
    [StringLength(15)] public string? Country { get; set; }
    [StringLength(24)] public string? HomePhone { get; set; }
    [Range(1, int.MaxValue)] public int? ReportsTo { get; set; }
    [Required, RegularExpression(@"^[a-zA-Z0-9_.-]{3,50}$")]
    public string Username { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[\s\S]*$")]
    public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "";

    public void Apply(Employee employee)
    {
        employee.FirstName = FirstName;
        employee.LastName = LastName;
        employee.Title = Title;
        employee.BirthDate = BirthDate;
        employee.HireDate = HireDate;
        employee.Address = Address;
        employee.City = City;
        employee.Country = Country;
        employee.HomePhone = HomePhone;
        employee.ReportsTo = ReportsTo;
    }
}

public class UpdateEmployeeRequest
{
    [Required, StringLength(10)] public string FirstName { get; set; } = "";
    [Required, StringLength(20)] public string LastName { get; set; } = "";
    [StringLength(30)] public string? Title { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime? HireDate { get; set; }
    [StringLength(60)] public string? Address { get; set; }
    [StringLength(15)] public string? City { get; set; }
    [StringLength(15)] public string? Country { get; set; }
    [StringLength(24)] public string? HomePhone { get; set; }
    [Range(1, int.MaxValue)] public int? ReportsTo { get; set; }

    public void Apply(Employee employee)
    {
        employee.FirstName = FirstName;
        employee.LastName = LastName;
        employee.Title = Title;
        employee.BirthDate = BirthDate;
        employee.HireDate = HireDate;
        employee.Address = Address;
        employee.City = City;
        employee.Country = Country;
        employee.HomePhone = HomePhone;
        employee.ReportsTo = ReportsTo;
    }
}

public record EmployeeListDto(
    int EmployeeId, string FirstName, string LastName, string? Title, DateTime? HireDate,
    string? City, string? Country, bool IsActive, bool HasUserAccount,
    string? Username, string? UserRole, bool? UserIsActive);

public record EmployeeDetailDto(
    int EmployeeId, string FirstName, string LastName, string? Title, DateTime? BirthDate,
    DateTime? HireDate, string? Address, string? City, string? Country, string? HomePhone,
    int? ReportsTo, bool IsActive, bool HasUserAccount, string? Username,
    string? UserRole, bool? UserIsActive);

public record PagedEmployeesResponse(
    IReadOnlyList<EmployeeListDto> Items, int Limit, int Offset, int TotalCount);
