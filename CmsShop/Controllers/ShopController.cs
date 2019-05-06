using CmsShop.Models.Data;
using CmsShop.Models.ViewsModels.Shop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CmsShop.Controllers
{
    public class ShopController : Controller
    {
        // GET: Shop
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Pages");
        }

        public PartialViewResult CategoryMenuPartial()
        {
            //deklaracja listy kategorii
            List<CategoryVM> categoryVMList;

            //inicjalizacja listy kategorii
            using (Db db = new Db())
            {
                categoryVMList = db.Categories
                                        .ToArray()
                                        .OrderBy(x => x.Sorting)
                                        .Select(x => new CategoryVM(x))
                                        .ToList();
            }
            return PartialView(categoryVMList);
        }

        // GET: Shop/Category/name
        [HttpGet]
        public ActionResult Category(string name)
        {
            //deklaracja listy produktów
            List<ProductVM> productsVMList;

            using (Db db = new Db())
            {
                //pobranie Id kategorii
                CategoryDTO categoryDTO = db.Categories.Where(x => x.Slug == name).FirstOrDefault();
                int categoryId = categoryDTO.Id;

                //inicjalizacja listy produktów
                productsVMList = db.Products
                                        .ToArray()
                                        .Where(x => x.CategoryId == categoryId)
                                        .Select(x => new ProductVM(x)).ToList();

                //pobranie nazwy kategorii
                var productCategory = db.Products.Where(x => x.CategoryId == categoryId).FirstOrDefault();
                ViewBag.CategoryName = productCategory.CategoryName;
            }

            return View(productsVMList);
        }

        // GET: Shop/produkt-szczegoly/name
        [ActionName("produkt-szczegoly")]
        public ActionResult ProductDetails(string name)
        {
            ProductDTO productDTO;
            ProductVM productVM;

            int productId = 0;

            using (Db db = new Db())
            {
                if (!db.Products.Any(x => x.Slug.Equals(name)))
                {
                    return RedirectToAction("Index", "Shop");
                }

                productDTO = db.Products.Where(x => x.Slug == name).FirstOrDefault();
                productId = productDTO.Id;

                productVM = new ProductVM(productDTO);
            }

            //pobranie galerii zdjec dla wybranego produktu
            productVM.GalleryImage = Directory.EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + productId + "/Gallery/Thumbs/"))
                                              .Select(fn => Path.GetFileName(fn));

            return View("ProductDetails", productVM);
        }
    }
}