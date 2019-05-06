using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CmsShop.Models.ViewsModels.Account
{
    public class OrdersForUserVM
    {
        [Display(Name = "Numer zamówienia")]
        public int OrderNumber { get; set; }
        [Display(Name = "Wartość zamówienia")]
        public decimal Total { get; set; }
        public Dictionary<string, int> ProductsAndQuantity { get; set; }
        [Display(Name = "Data zamówienia")]
        public DateTime CreatedAt { get; set; }
    }
}