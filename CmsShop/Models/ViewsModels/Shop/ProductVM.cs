using CmsShop.Models.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CmsShop.Models.ViewsModels.Shop
{
    public class ProductVM
    {
        public int Id { get; set; }
        [Required]
        [Display(Name = "Nazwa")]
        public string Name { get; set; }
        public string Slug { get; set; }
        [Required]
        [Display(Name = "Opis")]
        public string Description { get; set; }
        [Display(Name = "Cena")]
        public decimal Price { get; set; }
        public string CategoryName { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public string ImageName { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; }
        public IEnumerable<string> GalleryImage { get; set; }

        public ProductVM()
        {

        }

        public ProductVM(ProductDTO productDTO)
        {
            Id = productDTO.Id;
            Name = productDTO.Name;
            Slug = productDTO.Slug;
            Description = productDTO.Description;
            Price = productDTO.Price;
            CategoryName = productDTO.CategoryName;
            CategoryId = productDTO.CategoryId;
            ImageName = productDTO.ImageName;
        }
    }
}