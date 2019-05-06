using CmsShop.Models.Data;
using CmsShop.Models.ViewModels.Pages;
using CmsShop.Models.ViewsModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CmsShop.Controllers
{
    public class PagesController : Controller
    {
        // GET: Index/{pages}
        public ActionResult Index(string page = "")
        {
            //ustawienie adresu strony
            if (page == "")
                page = "home";

            //deklaracja
            PageVM pageVM;
            PageDTO pageDTO;

            //sprawdzenie czy strona istniej
            using (Db db = new Db())
            {
                if (!db.Pages.Any(x => x.Slug.Equals(page)))
                {
                    return RedirectToAction("Index", new { page = "" });
                }
            }

            //pobieranie pageDTO
            using (Db db = new Db())
            {
                pageDTO = db.Pages.Where(x => x.Slug == page).FirstOrDefault();
            }

            //ustawienie tytułu strony 
            ViewBag.PageTitle = pageDTO.Title;

            //sprawdzenie czy strona ma pasek boczny
            if (pageDTO.HasSidebar == true)
            {
                ViewBag.Sidebar = "Tak";
            }
            else
            {
                ViewBag.Sidebar = "Nie";
            }

            //inicjalizacja pabeVM
            pageVM = new PageVM(pageDTO);

            return View(pageVM);
        }

        // GET: Index/PagesMenuPartial
        public ActionResult PagesMenuPartial()
        {
            //deklaracja listy PageVM
            List<PageVM> pageVMList;

            //pobranie stron

            using (Db db = new Db())
            {
                pageVMList = db.Pages.ToArray()
                        .Where(x => x.Slug != "home")
                        .OrderBy(x => x.Sorting)
                        .Select(x => new PageVM(x)).ToList();
            }
            return PartialView(pageVMList);
        }

        // GET: Index/SidebarPartial
        public ActionResult SidebarPartial()
        {
            //deklaracja modelu
            SidebarVM model;

            //Inicjalizacja modelu
            using (Db db = new Db())
            {
                SidebarDTO sidebarDTO = db.Sidebar.Find(1);
                model = new SidebarVM(sidebarDTO);
            }

            return PartialView(model);
        }
    }
}