using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CmsShop.Areas.Admin.Models.ViewModels.Shop
{
    public class OrdersForAdminVM
    {
        [Display(Name = "Numer zamówienia")]
        public int OrderNumber { get; set; }
        [Display(Name = "Nazwa użytkownika")]
        public string Username { get; set; }
        [Display(Name = "Wartość zamówienia")]
        public decimal Total { get; set; }
        public Dictionary<string, int> ProductsAndQuantity { get; set; }
        [Display(Name = "Data zamówienia")]
        public DateTime CreatedAt { get; set; }
    }
}