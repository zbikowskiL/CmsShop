using CmsShop.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CmsShop.Models.ViewsModels.Pages
{
    public class SidebarVM
    {
        public int Id { get; set; }

        [AllowHtml] //trzeba dodać aby można było korzystać z CKeditora 
        public string Body { get; set; }

        public SidebarVM()
        {

        }

        public SidebarVM(SidebarDTO row)
        {
            Id = row.Id;
            Body = row.Body;
        }
    }
}