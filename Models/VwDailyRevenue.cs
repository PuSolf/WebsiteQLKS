using System;
using System.Collections.Generic;

namespace WebsiteQLKS.Models;

public partial class VwDailyRevenue
{
    public DateOnly? Date { get; set; }

    public decimal? Revenue { get; set; }

    public int? PaymentCount { get; set; }
}
