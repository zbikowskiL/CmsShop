using ClosedXML.Excel;
using CmsShop.Areas.Admin.Models.ViewModels.Shop;
using CmsShop.Models.Data;
using CmsShop.Models.ViewsModels.Shop;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace CmsShop.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ShopController : Controller
    {

        // GET: Admin/Shop/Categories
        [HttpGet]
        public ActionResult Categories()
        {
            List<CategoryVM> categorViewModelsList;

            using (Db db = new Db())
            {
                categorViewModelsList = db.Categories
                                        .ToArray()
                                                .OrderBy(x => x.Sorting)
                                                .Select(x => new CategoryVM(x)).ToList();
            }

            return View(categorViewModelsList);
        }

        // POST: Admin/Shop/Categories
        [HttpPost]
        public string AddNewCategory(string catName)
        {
            //Deklaracja id
            string id;

            using (Db db = new Db())
            {
                //sprawdzenie czy kategoria juz istnieje
                if (db.Categories.Any(x => x.Name == catName))
                    return "tytulzajety";

                //inicjalizacja CategoryDTO
                CategoryDTO categoryDTO = new CategoryDTO();
                categoryDTO.Name = catName;
                categoryDTO.Slug = catName.Replace(" ", "-").ToLower();
                categoryDTO.Sorting = 1000;

                db.Categories.Add(categoryDTO);
                db.SaveChanges();

                //pobieranie id dodawanej kategorii
                id = categoryDTO.Id.ToString();
            }
            return id;
        }

        [HttpPost]
        public ActionResult ReorderCategories(int[] id)
        {
            int count = 1;

            using (Db db = new Db())
            {

                CategoryDTO categoryDTO = new CategoryDTO();

                foreach (var categoryId in id)
                {
                    categoryDTO = db.Categories.Find(categoryId);
                    categoryDTO.Sorting = count;

                    db.SaveChanges();

                    count++;
                }

            }

            return View();
        }
        // GET: Admin/Shop/DeleteCategory
        [HttpGet]
        public ActionResult DeleteCategory(int id)
        {
            using (Db db = new Db())
            {
                CategoryDTO categoryDTO = new CategoryDTO();

                categoryDTO = db.Categories.Find(id);

                db.Categories.Remove(categoryDTO);
                db.SaveChanges();
            }
            return RedirectToAction("Categories");
        }
        // POST: //Admin/Shop/RenameCategory
        [HttpPost]
        public string RenameCategory(string newCategoryName, int id)
        {
            using (Db db = new Db())
            {
                if (db.Categories.Any(x => x.Name == newCategoryName))
                    return "tytulzajety";

                CategoryDTO categoryDTO = db.Categories.Find(id);

                categoryDTO.Name = newCategoryName;
                categoryDTO.Slug = newCategoryName.Replace(" ", "-").ToLower();

                db.SaveChanges();
            }

            return "ok";
        }
        // GET: Admin/Shop/AddProduct
        [HttpGet]
        public ActionResult AddProduct()
        {
            ProductVM productVM = new ProductVM();

            using (Db db = new Db())
            {
                productVM.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
            }

            return View(productVM);
        }

        // POST: Admin/Shop/AddProduct
        [HttpPost]
        public ActionResult AddProduct(ProductVM model, HttpPostedFileBase file)
        {
            if (!ModelState.IsValid)
            {
                using (Db db = new Db())
                {
                    //Pobieramy na nowo kategorie do dropDownList
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    return View(model);
                }
            }

            //sprawdzanie czy nazwa produktu jest unikalna
            using (Db db = new Db())
            {
                if (db.Products.Any(x => x.Name == model.Name))
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    ModelState.AddModelError("", "Ta nazwa jest już zajeta!");
                    return View(model);
                }
            }

            //deklaracja id produktu
            int id = 0;

            //Dodanie nowego produktu i zapis w bazie
            using (Db db = new Db())
            {
                ProductDTO productDTO = new ProductDTO();
                productDTO.Name = model.Name;
                productDTO.Slug = model.Name.Replace(" ", "-").ToLower();
                productDTO.Description = model.Description;
                productDTO.Price = model.Price;
                productDTO.CategoryId = model.CategoryId;

                //pobranie nazwy kategori z bazy
                CategoryDTO categoryDTO = new CategoryDTO();
                categoryDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
                productDTO.CategoryName = categoryDTO.Name;

                db.Products.Add(productDTO);
                db.SaveChanges();

                //pobranie id danego produktu
                id = productDTO.Id;
            }

            #region dodawanie obrazka

            //Utworzenie potrzebnych ścieżek katalogów
            //stworzenie folderu CmsShop\Images\Uploads\   @"\" odnosi się do głównego katalogu CmsShop
            var originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));

            var pathString1 = Path.Combine(originalDirectory.ToString(), "Products");
            var pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());
            var pathString3 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");
            var pathString4 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
            var pathString5 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");

            if (!Directory.Exists(pathString1))
                Directory.CreateDirectory(pathString1);
            if (!Directory.Exists(pathString2))
                Directory.CreateDirectory(pathString2);
            if (!Directory.Exists(pathString3))
                Directory.CreateDirectory(pathString3);
            if (!Directory.Exists(pathString4))
                Directory.CreateDirectory(pathString4);
            if (!Directory.Exists(pathString5))
                Directory.CreateDirectory(pathString5);

            if (file != null && file.ContentLength > 0)
            {
                //sprawdzenie poprawności rozszerzenia pliku
                string extension = file.ContentType.ToLower();
                if (extension != "image/jpg" &&
                    extension != "image/jpeg" &&
                    extension != "image/pjpeg" &&
                    extension != "image/gif" &&
                    extension != "image/png" &&
                    extension != "image/x-png")
                {
                    using (Db db = new Db())
                    {
                        model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                        ModelState.AddModelError("", "Obraz nie został przesłany - nieprawidłowe rozszerzenie obrazu!");
                        return View(model);
                    }
                }

                //inicjalizacja nazwy obrazka
                string fileName = file.FileName;

                //zapis nazwy obrazka do bazy
                using (Db db = new Db())
                {
                    ProductDTO productDTO = db.Products.Find(id);
                    productDTO.ImageName = fileName;
                    db.SaveChanges();
                }

                var path = string.Format("{0}\\{1}", pathString2, fileName);
                var path2 = string.Format("{0}\\{1}", pathString3, fileName);

                //zapis oryginalnego obrazka
                file.SaveAs(path);

                //zapis miniaturki
                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200);
                img.Save(path2);

                TempData["SM"] = "Dodałeś nowy produkt!";
                #endregion
            }

            return RedirectToAction("AddProduct");
        }

        // GET: Admin/Shop/Products
        [HttpGet]
        public ActionResult Products(int? page, int? catId)
        {
            //Deklaracja listy produktów

            List<ProductVM> listOfProductsVM;

            //Ustawiamy nr strony
            int pageNumber = page ?? 1; //jeżeli page == 0 lub nul ustawi 1

            using (Db db = new Db())
            {
                //inicjalizacja listy produktów
                listOfProductsVM = db.Products.ToArray()
                                        .Where(x => catId == null || catId == 0 || x.CategoryId == catId)
                                        .Select(x => new ProductVM(x))
                                        .ToList();

                //lista kategorii do DropDownList
                ViewBag.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");

                //Ustawiam wybrana kategorię
                ViewBag.SelectedCat = catId.ToString();

                //Ustawienie stronnicowania dzieki nugetowi PagedList.MVC
                var onePageOfProducts = listOfProductsVM.ToPagedList(pageNumber, 3);
                ViewBag.OnePageOfProducts = onePageOfProducts;

            }

            return View(listOfProductsVM);
        }


        // GET: Admin/Shop/EditProduct/id
        [HttpGet]
        public ActionResult EditProduct(int id)
        {

            //deklaracja produktu do edycji
            ProductVM productVM;

            using (Db db = new Db())
            {
                ProductDTO productDTO;

                productDTO = db.Products.Find(id);

                if (productDTO == null)
                {
                    return Content("Ten produkt nie istnieje.");
                }

                productVM = new ProductVM(productDTO);

                //pobranie lity kategorii
                productVM.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");

                //ustawiamy zdjęcie
                productVM.GalleryImage = Directory.EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                    .Select(fn => Path.GetFileName(fn));

            }

            return View(productVM);
        }

        // POST: Admin/Shop/EditProduct
        [HttpPost]
        public ActionResult EditProduct(ProductVM productVM, HttpPostedFileBase file)
        {
            //pobranie id produktu
            int id = productVM.Id;

            //pobranie kategorii dla listy rozwijanej
            using (Db db = new Db())
            {
                productVM.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
            }

            //ustawiamy zdjęcie
            productVM.GalleryImage = Directory.EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                .Select(fn => Path.GetFileName(fn));

            if (!ModelState.IsValid)
            {
                return View(productVM);
            }


            //sprawdzenie unikalności nazwy produktu
            using (Db db = new Db())
            {
                if (db.Products.Where(x => x.Id != id).Any(x => x.Name == productVM.Name))
                {
                    ModelState.AddModelError("", "Ta nazwa produktu jest zajeta.");
                    return View(productVM);
                }
            }

            //edycja produktu i zapis na bazie

            using (Db db = new Db())
            {
                ProductDTO productDTO = db.Products.Find(id);

                productDTO.Name = productVM.Name;
                productDTO.Slug = productVM.Name.Replace(" ", "-").ToLower();
                productDTO.Price = productVM.Price;
                productDTO.Description = productVM.Description;
                productDTO.CategoryId = productVM.CategoryId;
                productDTO.ImageName = productVM.ImageName;

                CategoryDTO categoryDTO = db.Categories.FirstOrDefault(x => x.Id == productVM.CategoryId);
                productDTO.CategoryName = categoryDTO.Name;

                db.SaveChanges();
            }

            TempData["SM"] = "Wyedytowałeś produkt.";

            #region Image Upload

            if (file != null && file.ContentLength > 0)
            {
                //sprawdzenie rozszerzenia pliku obrazka
                string extension = file.ContentType.ToLower();
                if (extension != "image/jpg" &&
                    extension != "image/jpeg" &&
                    extension != "image/pjpeg" &&
                    extension != "image/gif" &&
                    extension != "image/png" &&
                    extension != "image/x-png")
                {
                    using (Db db = new Db())
                    {
                        productVM.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                        ModelState.AddModelError("", "Obraz nie został przesłany - nieprawidłowe rozszerzenie obrazu!");
                        return View(productVM);
                    }
                }

                //utworzenie potrzebnej struktury katalogów
                var originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));

                var pathString1 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());
                var pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");

                //usuwanie starych plików z katalogu
                DirectoryInfo di1 = new DirectoryInfo(pathString1);
                DirectoryInfo di2 = new DirectoryInfo(pathString2);

                foreach (var file1 in di1.GetFiles())
                {
                    file1.Delete();
                }

                foreach (var file2 in di2.GetFiles())
                {
                    file2.Delete();
                }

                //Zapis nazwy obrazka a bazie
                string imageName = file.FileName;
                using (Db db = new Db())
                {
                    ProductDTO productDTO = db.Products.Find(id);
                    productDTO.ImageName = imageName;
                    db.SaveChanges();
                }

                //Zapis nowych plików
                var path1 = string.Format("{0}\\{1}", pathString1, imageName);
                var path2 = string.Format("{0}\\{1}", pathString2, imageName);

                file.SaveAs(path1);

                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200);
                img.Save(path2);
            }
            #endregion

            return RedirectToAction("EditProduct");
        }

        // GET: Admin/Shop/DeleteProduct/id
        [HttpGet]
        public ActionResult DeleteProduct(int id)
        {
            using (Db db = new Db())
            {
                ProductDTO productDTO = db.Products.Find(id);
                db.Products.Remove(productDTO);
                db.SaveChanges();
            }

            //usunięcie folderu produktów
            var originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));
            var pathString = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());

            if (Directory.Exists(pathString))
                Directory.Delete(pathString, true);

            return RedirectToAction("Products");
        }

        // POST: Admin/Shop/SaveGalleryImages/id
        [HttpPost]
        public void SaveGalleryImages(int id)
        {
            //petla po obrazkach w galerii  Request.Files <- pochodzi z dodanych na stronie obrazków [Dropzone]
            foreach (string fileName in Request.Files)
            {
                //inicjalizacja 
                HttpPostedFileBase file = Request.Files[fileName];

                //sprawdzenie przesyłanego obrazka, czy nie jest pusty

                if (file != null && file.ContentLength > 0)
                {
                    //utworzenie potrzebnej struktury katalogów
                    var originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));

                    string pathString1 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
                    string pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");

                    var path1 = string.Format("{0}\\{1}", pathString1, file.FileName);
                    var path2 = string.Format("{0}\\{1}", pathString2, file.FileName);

                    //zapis obrazków
                    file.SaveAs(path1);
                    WebImage img = new WebImage(file.InputStream);
                    img.Resize(200, 200);
                    img.Save(path2);
                }
            }
        }

        // POST: Admin/Shop/DeleteImage/id
        [HttpPost]
        public void DeleteImage(int id, string imageName)
        {
            string fullPath1 = Request.MapPath("~/Images/Uploads/Products/" + id.ToString() + "/Gallery/" + imageName);
            string fullPath2 = Request.MapPath("~/Images/Uploads/Products/" + id.ToString() + "/Gallery/Thumbs/" + imageName);

            if (System.IO.File.Exists(fullPath1))
                System.IO.File.Delete(fullPath1);

            if (System.IO.File.Exists(fullPath2))
                System.IO.File.Delete(fullPath2);
        }

        // GET: Admin/Shop/Orders
        public ActionResult Orders()
        {
            //inicjalizacja listy ordersForAdmin
            List<OrdersForAdminVM> ordersForAdminVM = new List<OrdersForAdminVM>();

            using (Db db = new Db())
            {
                //pobieranie wszystkich zamówień
                List<OrderVM> orders = db.Orders.ToArray().Select(x => new OrderVM(x)).ToList();

                foreach (var order in orders)
                {
                    //inicjalizacja słownika produktów
                    Dictionary<string, int> productsAndQuantity = new Dictionary<string, int>();

                    decimal total = 0m;

                    //inicjalizacja OrdersDetalisDTO
                    List<OrderDetailsDTO> orderDetailsDTOlist = db.OrderDetails.Where(x => x.OrderId == order.OrderId).ToList();

                    //pobieranie uzytkownika
                    UserDTO user = db.Users.Where(x => x.Id == order.UserId).FirstOrDefault();
                    string userName = user.UserName;

                    foreach (var orderDetails in orderDetailsDTOlist)
                    {
                        //pobranie produktu
                        ProductDTO product = db.Products.Where(x => x.Id == orderDetails.ProductId).FirstOrDefault();

                        //pobranie ceny produktu
                        decimal price = product.Price;

                        //pobranie nazwy produktu
                        string productName = product.Name;

                        //dodanie produktu do słownika
                        productsAndQuantity.Add(productName, orderDetails.Quantity);

                        //całkowita wartość zamówienia
                        total += orderDetails.Quantity * price;
                    }

                    ordersForAdminVM.Add(new OrdersForAdminVM
                    {
                        OrderNumber = order.OrderId,
                        Username = userName,
                        Total = total,
                        ProductsAndQuantity = productsAndQuantity,
                        CreatedAt = order.CreatedAt
                    });
                }
            }

            return View(ordersForAdminVM);
        }


        #region methods for PDF Creator
        public FileResult CreatedPDF()
        {
            MemoryStream workStream = new MemoryStream();
            StringBuilder status = new StringBuilder();

            DateTime dTime = DateTime.Now;

            //file name to be created
            string strPdfFileName = string.Format("Zamówienia_" + dTime.ToString("yyyyMMdd") + ".pdf");

            Document doc = new Document();
            doc.SetMargins(5f, 5f, 5f, 5f);

            //Created PDF Table with 3 columns
            PdfPTable tableLayout = new PdfPTable(5);
            doc.SetMargins(0f, 0f, 0f, 0f);

            //file will created in this path
            string strAttachment = Server.MapPath("~/Downloadss/" + strPdfFileName);

            PdfWriter.GetInstance(doc, workStream).CloseStream = false;
            doc.Open();

            //Add Content PDF
            doc.Add(Add_Content_To_PDF(tableLayout));

            doc.Close();

            byte[] byteInfo = workStream.ToArray();
            workStream.Write(byteInfo, 0, byteInfo.Length);
            workStream.Position = 0;

            return File(workStream, "application/pdf", strPdfFileName);
        }

        protected PdfPTable Add_Content_To_PDF(PdfPTable tableLayout)
        {
            float[] headers = { 30, 50, 50, 50, 50 };
            tableLayout.SetWidths(headers);
            tableLayout.WidthPercentage = 100;
            tableLayout.HeaderRows = 1;

            //inicjalizacja listy ordersForAdmin
            List<OrdersForAdminVM> ordersForAdminVM = new List<OrdersForAdminVM>();

            using (Db db = new Db())
            {
                //pobieranie wszystkich zamówień
                List<OrderVM> orders = db.Orders.ToArray().Select(x => new OrderVM(x)).ToList();

                foreach (var order in orders)
                {
                    decimal total = 0m;
                    //inicjalizacja słownika produktów
                    Dictionary<string, int> productsAndQuantityForPDF = new Dictionary<string, int>();
                    //inicjalizacja OrdersDetalisDTO
                    List<OrderDetailsDTO> orderDetailsDTOlist = db.OrderDetails.Where(x => x.OrderId == order.OrderId).ToList();

                    //pobieranie uzytkownika
                    UserDTO user = db.Users.Where(x => x.Id == order.UserId).FirstOrDefault();
                    string userName = user.UserName;

                    foreach (var orderDetails in orderDetailsDTOlist)
                    {
                        //pobranie produktu
                        ProductDTO product = db.Products.Where(x => x.Id == orderDetails.ProductId).FirstOrDefault();

                        //pobranie ceny produktu
                        decimal price = product.Price;

                        //pobranie nazwy produktu
                        string productName = product.Name;

                        //dodanie produktu do słownika
                        productsAndQuantityForPDF.Add(productName, orderDetails.Quantity);

                        //całkowita wartość zamówienia
                        total += orderDetails.Quantity * price;
                    }

                    ordersForAdminVM.Add(new OrdersForAdminVM
                    {
                        OrderNumber = order.OrderId,
                        Username = userName,
                        Total = total,
                        ProductsAndQuantity = productsAndQuantityForPDF,
                        CreatedAt = order.CreatedAt
                    });
                }

                tableLayout.AddCell(new PdfPCell(new Phrase("Zwmówienia",
                    new Font(Font.FontFamily.HELVETICA, 8, 1, new iTextSharp.text.BaseColor(0, 0, 0))))
                {
                    Colspan = 12,
                    Border = 0,
                    PaddingBottom = 5,
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
            }

            //Add header
            AddCellHeader(tableLayout, "Numer zamówienia.");
            AddCellHeader(tableLayout, "Nazwa użytkownika");
            AddCellHeader(tableLayout, "Szczegóły zamówienia");
            AddCellHeader(tableLayout, "Data zamówienia");
            AddCellHeader(tableLayout, "Wartośc zamówienia");

            int index = 1;
            string productDetails = "";
            foreach (var order in ordersForAdminVM)
            {
                AddCellBody(tableLayout, order.OrderNumber.ToString());
                AddCellBody(tableLayout, order.Username);

                foreach (var item in order.ProductsAndQuantity)
                {
                    if (item.Value == 0)
                    {
                        productDetails = "Brak szczegółów zamówienia";
                    }
                    else if (order.ProductsAndQuantity.Count == 1)
                    {
                        productDetails = string.Format(item.Key + " x " + item.Value.ToString());
                    }
                    else
                    {
                        string details2 = string.Format(item.Key + " x " + item.Value.ToString() + Environment.NewLine);
                        productDetails += details2;
                    }

                }
                AddCellBody(tableLayout, productDetails);
                AddCellBody(tableLayout, order.CreatedAt.ToString());
                if (order.Total == 0)
                {
                    AddCellBody(tableLayout, "0,00");
                }
                else
                {
                    AddCellBody(tableLayout, order.Total.ToString());
                }

                index++;
            }

            return tableLayout;
        }

        private static void AddCellHeader(PdfPTable tableLayout, string cellText)
        {
            tableLayout.AddCell(new PdfPCell(new Phrase(cellText, new Font(Font.FontFamily.HELVETICA, 8, 1, iTextSharp.text.BaseColor.YELLOW)))
            {
                HorizontalAlignment = Element.ALIGN_LEFT,
                Padding = 5,
                BackgroundColor = new iTextSharp.text.BaseColor(128, 0, 0)
            });
        }

        private static void AddCellBody(PdfPTable tableLayout, string cellText)
        {
            tableLayout.AddCell(new PdfPCell(new Phrase(cellText, new Font(Font.FontFamily.HELVETICA, 8, 1, iTextSharp.text.BaseColor.BLACK)))
            {
                HorizontalAlignment = Element.ALIGN_LEFT,
                Padding = 5,
                BackgroundColor = new iTextSharp.text.BaseColor(255, 255, 255)
            });
        }
        #endregion

        #region Excel from database
        public FileResult ExportToExcel()
        {
            //inicjalizacja listy ordersForAdmin
            List<OrdersForAdminVM> ordersForAdminVM = new List<OrdersForAdminVM>();

            using (Db db = new Db())
            {
                //pobieranie wszystkich zamówień
                List<OrderVM> orders = db.Orders.ToArray().Select(x => new OrderVM(x)).ToList();

                foreach (var order in orders)
                {
                    decimal total = 0m;
                    //inicjalizacja słownika produktów
                    Dictionary<string, int> productsAndQuantityForPDF = new Dictionary<string, int>();
                    //inicjalizacja OrdersDetalisDTO
                    List<OrderDetailsDTO> orderDetailsDTOlist = db.OrderDetails.Where(x => x.OrderId == order.OrderId).ToList();

                    //pobieranie uzytkownika
                    UserDTO user = db.Users.Where(x => x.Id == order.UserId).FirstOrDefault();
                    string userName = user.UserName;

                    foreach (var orderDetails in orderDetailsDTOlist)
                    {
                        //pobranie produktu
                        ProductDTO product = db.Products.Where(x => x.Id == orderDetails.ProductId).FirstOrDefault();

                        //pobranie ceny produktu
                        decimal price = product.Price;

                        //pobranie nazwy produktu
                        string productName = product.Name;

                        //dodanie produktu do słownika
                        productsAndQuantityForPDF.Add(productName, orderDetails.Quantity);

                        //całkowita wartość zamówienia
                        total += orderDetails.Quantity * price;
                    }

                    ordersForAdminVM.Add(new OrdersForAdminVM
                    {
                        OrderNumber = order.OrderId,
                        Username = userName,
                        Total = total,
                        ProductsAndQuantity = productsAndQuantityForPDF,
                        CreatedAt = order.CreatedAt
                    });
                }

                DataTable dataTable = new DataTable("Lista stron");
                dataTable.Columns.AddRange(new DataColumn[5] {
                new DataColumn("Numer zamówienia"),
                new DataColumn("Nazwa użytkownika"),
                new DataColumn("Szczegóły zamówienia"),
                new DataColumn("Data zamówienia"),
                new DataColumn("Wartośc zamówienia")
            });

                foreach (var order in ordersForAdminVM)
                {
                    string productDetails = "";
                    foreach (var item in order.ProductsAndQuantity)
                    {
                        if (item.Value == 0)
                        {
                            productDetails = "Brak szczegółów zamówienia";
                        }
                        else if (order.ProductsAndQuantity.Count == 1)
                        {
                            productDetails = string.Format(item.Key + " x " + item.Value.ToString());
                        }
                        else
                        {
                            string details2 = string.Format(item.Key + " x " + item.Value.ToString() + Environment.NewLine);
                            productDetails += details2;
                        }
                    }

                    dataTable.Rows.Add(order.OrderNumber, order.Username, productDetails, order.CreatedAt, order.Total);
                }

                using (XLWorkbook wb = new XLWorkbook())
                {
                    wb.Worksheets.Add(dataTable);
                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);
                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Zamówienia.xlsx");
                    }
                }
            }
            #endregion
        }
    }
}
