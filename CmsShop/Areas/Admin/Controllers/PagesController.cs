using ClosedXML.Excel;
using CmsShop.Models.Data;
using CmsShop.Models.ViewModels.Pages;
using CmsShop.Models.ViewsModels.Pages;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CmsShop.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PagesController : Controller
    {
        // GET: Admin/Pages
        [HttpGet]
        public ActionResult Index()
        {

            //Declaration of the list of pages
            List<PageVM> pagesList;

            //Initialization list
            using (Db db = new Db())
            {
                pagesList = db.Pages.ToArray().OrderBy(x => x.Sorting).Select(x => new PageVM(x)).ToList();
            }

            //Return Side to View
            return View(pagesList);
        }

        // GET: Admin/Pages/AddPage
        [HttpGet]
        public ActionResult AddPage()
        {
            return View();
        }


        // POST: Admin/Pages/AddPage
        [HttpPost]
        public ActionResult AddPage(PageVM pageVM)
        {
            //Checked model state
            if (!ModelState.IsValid)
            {
                return View(pageVM);
            }

            using (Db db = new Db())
            {
                string slug;
                PageDTO pageDTO = new PageDTO();
                pageDTO.Title = pageVM.Title;

                //when we don't have website address we assign the title
                if (string.IsNullOrWhiteSpace(pageVM.Slug))
                {
                    slug = pageVM.Title.Replace(" ", "-").ToLower();
                }
                else
                {
                    slug = pageVM.Slug.Replace(" ", "-").ToLower();
                }

                //we prevent-zapobiegamy adding the same website name
                if (db.Pages.Any(x => x.Title == pageVM.Title) || (db.Pages.Any(x => x.Slug == slug)))
                {
                    ModelState.AddModelError("", "Ten tytuł lub adres strony już istnieje.");
                    return View(pageVM);
                }

                pageDTO.Title = pageVM.Title;
                pageDTO.Slug = slug;
                pageDTO.Body = pageVM.Body;
                pageDTO.HasSidebar = pageVM.HasSidebar;
                pageDTO.Sorting = 1000;

                db.Pages.Add(pageDTO);
                db.SaveChanges();

            }
            TempData["SM"] = "Dodałeś nową stronę!";

            return RedirectToAction("AddPage");
        }

        // GET: Admin/Pages/EditPage
        [HttpGet]
        public ActionResult EditPage(int id)
        {
            PageVM pageVM;

            using (Db db = new Db())
            {

                PageDTO pageDTO = db.Pages.Find(id);

                if (pageDTO == null)
                {
                    return Content("Strona nie istnieje.");
                }
                pageVM = new PageVM(pageDTO);
            }
            return View(pageVM);
        }

        // POST: Admin/Pages/EditPage/
        [HttpPost]
        public ActionResult EditPage(PageVM pageVM)
        {
            if (!ModelState.IsValid)
            {
                return View(pageVM);
            }

            using (Db db = new Db())
            {
                int id = pageVM.Id;
                string slug = "home";

                //Download pages from databasse to edit
                PageDTO pageDTO = db.Pages.Find(id);

                //changed slug
                if (pageVM.Slug != "home")
                {
                    if (string.IsNullOrWhiteSpace(pageVM.Slug))
                    {
                        slug = pageVM.Title.Replace(" ", "-").ToLower();
                    }
                    else
                    {
                        slug = pageVM.Slug.Replace(" ", "-").ToLower();
                    }
                }

                //checking uniqueness website title and address
                if (db.Pages.Where(x => x.Id != id).Any(x => x.Title == pageVM.Title) ||
                        db.Pages.Where(x => x.Id != id).Any(x => x.Slug == slug))
                {
                    ModelState.AddModelError("", "Strona lub adres strony już istnieje.");
                    return View(pageVM);
                }

                //changed rest pageDTO
                pageDTO.Title = pageVM.Title;
                pageDTO.Slug = slug;
                pageDTO.Body = pageVM.Body;
                pageDTO.HasSidebar = pageVM.HasSidebar;

                //save edit website to database
                db.SaveChanges();
            }

            //Message settings
            TempData["SM"] = "Wyedytowałeś stronę!";

            return RedirectToAction("EditPage");
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            PageVM model;

            using (Db db = new Db())
            {
                PageDTO pageDTO = db.Pages.Find(id);

                if (pageDTO == null)
                {
                    return Content("Strona o podanym id nie istnieje!");
                }

                model = new PageVM(pageDTO);
            }
            return View(model);
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            using (Db db = new Db())
            {
                PageDTO pageDTO = db.Pages.Find(id);

                db.Pages.Remove(pageDTO);

                db.SaveChanges();

            }
            return RedirectToAction("Index", "Pages");
        }

        [HttpPost]
        public ActionResult ReorderPages(int[] id)
        {
            //sortowanie stron na bazie
            using (Db db = new Db())
            {
                int count = 1;
                PageDTO pageDTO;

                foreach (var pageId in id)
                {
                    pageDTO = db.Pages.Find(pageId);
                    pageDTO.Sorting = count;

                    db.SaveChanges();
                    count++;
                }
            }
            return View();
        }

        // GET: Admin/Pages/EditSidebar
        [HttpGet]
        public ActionResult EditSidebar()
        {
            //deklaracja View modelu Sidebar
            SidebarVM sidebarVM;

            using (Db db = new Db())
            {
                //Pobieranie sidebara strony, narazie jest tylko jeden dla wszystkich stron dlatego też przyjmuje Find(1)
                SidebarDTO sidebarDTO = db.Sidebar.Find(1);

                //Inicjalizacja modelu z przypisaniem wartości z bazydanych
                sidebarVM = new SidebarVM(sidebarDTO);
            }

            return View(sidebarVM);
        }

        // POST: Admin/Pages/EditSidebar
        [HttpPost]
        public ActionResult EditSidebar(SidebarVM sidebarVM)
        {
            using (Db db = new Db())
            {
                SidebarDTO sidebarDTO = db.Sidebar.Find(1);

                sidebarDTO.Body = sidebarVM.Body;

                db.SaveChanges();
            }

            TempData["SM"] = "Zmodyfikowałeś pasek boczny.";

            return RedirectToAction("EditSidebar");
        }

        #region methods for PDF Creator
        public FileResult CreatedPDF()
        {
            MemoryStream workStream = new MemoryStream();
            StringBuilder status = new StringBuilder();

            DateTime dTime = DateTime.Now;

            //file name to be created
            string strPdfFileName = string.Format("Strony_Internetowe_" + dTime.ToString("yyyyMMdd") + ".pdf");

            Document doc = new Document();
            doc.SetMargins(5f, 5f, 5f, 5f);

            //Created PDF Table with 3 columns
            PdfPTable tableLayout = new PdfPTable(4);
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
            float[] headers = { 50, 50, 50, 50 };
            tableLayout.SetWidths(headers);
            tableLayout.WidthPercentage = 100;
            tableLayout.HeaderRows = 1;

            List<PageVM> pagesList;

            using (Db db = new Db())
            {
                pagesList = db.Pages.ToArray().OrderBy(x => x.Sorting).Select(x => new PageVM(x)).ToList();
            }

            tableLayout.AddCell(new PdfPCell(new Phrase("Strony internetowe",
                new Font(Font.FontFamily.HELVETICA, 8, 1, new iTextSharp.text.BaseColor(0, 0, 0))))
            {
                Colspan = 12,
                Border = 0,
                PaddingBottom = 5,
                HorizontalAlignment = Element.ALIGN_CENTER
            });

            //Add header

            AddCellHeader(tableLayout, "Lp.");
            AddCellHeader(tableLayout, "Tytuł");
            AddCellHeader(tableLayout, "Adres");
            AddCellHeader(tableLayout, "Pasek boczny");

            int index = 1;

            foreach (var page in pagesList)
            {
                AddCellBody(tableLayout, index.ToString());
                AddCellBody(tableLayout, page.Title);
                AddCellBody(tableLayout, page.Slug);

                if (page.HasSidebar)
                {
                    AddCellBody(tableLayout, "X");
                }
                else
                {
                    AddCellBody(tableLayout, "-");
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

        #region method to excel export
        public FileResult ExportToExcel()
        {
            //Declaration of the list of pages
            List<PageVM> pagesList;

            //Initialization list
            using (Db db = new Db())
            {
                pagesList = db.Pages.ToArray().OrderBy(x => x.Sorting).Select(x => new PageVM(x)).ToList();
            }

            DataTable dataTable = new DataTable("Lista stron");
            dataTable.Columns.AddRange(new DataColumn[3] {
                new DataColumn("Tytuł strony"),
                new DataColumn("Adres strony"),
                new DataColumn("Pasek boczny")
            });

            foreach (var page in pagesList)
            {
                if (page.HasSidebar)
                {
                    dataTable.Rows.Add(page.Title, page.Slug, "X");
                }
                else
                {
                    dataTable.Rows.Add(page.Title, page.Slug, "-");
                }
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dataTable);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Strony.xlsx");
                }
            }
        }
        #endregion
    }
}