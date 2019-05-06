using CmsShop.Models.Data;
using System;

namespace CmsShop.Models.ViewsModels.Shop
{
    public class OrderVM
    {
        public OrderVM()
        {

        }

        public OrderVM(OrderDTO row)
        {
            OrderId = row.OrderId;
            UserId = row.UserId;
            CreatedAt = row.CreatedAt;
        }

        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}