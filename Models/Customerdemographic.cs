using System;
using System.Collections.Generic;

namespace Equipo5.Api.Models;

public partial class Customerdemographic
{
    public string CustomerTypeId { get; set; } = null!;

    public string? CustomerDesc { get; set; }

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
