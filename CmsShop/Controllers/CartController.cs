using CmsShop.Models.Data;
using CmsShop.Models.ViewsModels.Cart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace CmsShop.Controllers
{
    public class CartController : Controller
    {
        // GET: Cart
        public ActionResult Index()
        {
            //inicjalizacja koszyka
            var cart = Session["cart"] as List<CartVM> ?? new List<CartVM>();

            if (cart.Count == 0 || Session["cart"] == null)
            {
                ViewBag.Message = "Twój koszyk jest pusty.";
                return View();
            }

            decimal totalPraice = 0m;

            foreach (var item in cart)
            {
                totalPraice += item.Total;
            }

            ViewBag.GrandTotal = totalPraice;
            return View(cart);
        }

        public PartialViewResult CartPartial()
        {
            //inicjalizacja CartVM
            CartVM cartVM = new CartVM();

            //inicjalizacja ilosci i ceny w koszyku
            int quantity = 0;
            decimal price = 0;

            //sprawdzenie czy są dane koszyka w sesji
            if (Session["cart"] != null)
            {
                //pobieranie wartosci ceny i ilosci z sesji
                var list = (List<CartVM>)Session["cart"];

                foreach (var item in list)
                {
                    quantity += item.Quantity;
                    price += item.Quantity * item.Price;
                }

                cartVM.Quantity = quantity;
                cartVM.Price = price;
            }
            else
            {
                quantity = 0;
                price = 0m; // m określa decimal
            }

            return PartialView(cartVM);
        }

        // GET: Cart/AddToCartPartial
        [HttpGet]
        public ActionResult AddToCartPartial(int id)
        {
            List<CartVM> cartList = Session["cart"] as List<CartVM> ?? new List<CartVM>();

            CartVM cartVM = new CartVM();

            using (Db db = new Db())
            {
                ProductDTO product = db.Products.Find(id);

                //sprawdzenie czy w koszyku jest juz taki produkt
                var productInCart = cartList.FirstOrDefault(x => x.ProductId == id);

                //w zaleznosci od tego czy produkt jest w koszyku dodajemy go lub zwiekszamy ilosc
                if (productInCart == null)
                {
                    cartList.Add(new CartVM()
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = 1,
                        Price = product.Price,
                        Image = product.ImageName
                    });
                }
                else
                {
                    productInCart.Quantity++;
                }
            }

            //pobieranie całkowitej wartości i ceny i dodawanie do modelu
            int quantity = 0;
            decimal price = 0m;

            foreach (var item in cartList)
            {
                quantity += item.Quantity;
                price += item.Price * item.Quantity;
            }

            cartVM.Quantity = quantity;
            cartVM.Price = price;

            //zapis do sesji
            Session["cart"] = cartList;

            return PartialView(cartVM);
        }

        public JsonResult IncrementProduct(int productId)
        {
            //inicjalizacja listy produktów w koszyku
            List<CartVM> cartList = Session["cart"] as List<CartVM>;

            //pobieranie cartVM
            CartVM cartVM = cartList.FirstOrDefault(x => x.ProductId == productId);

            //zwiekszenie ilosci produktu qty
            cartVM.Quantity++;

            //przygotowanie danych do JSON'a
            var result = new { qty = cartVM.Quantity, price = cartVM.Price };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult DecrementProduct(int productId)
        {
            //inicjalizacja listy produktów w koszyku
            List<CartVM> cartList = Session["cart"] as List<CartVM>;

            //pobieranie cartVM
            CartVM cartVM = cartList.FirstOrDefault(x => x.ProductId == productId);

            //zmniejszanie ilosci produktu qty
            if (cartVM.Quantity > 1)
            {
                cartVM.Quantity--;
            }
            else
            {
                cartVM.Quantity = 0;
                cartList.Remove(cartVM);
            }

            //przygotowanie danych do JSON'a
            var result = new { qty = cartVM.Quantity, price = cartVM.Price };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public void RemovetProduct(int productId)
        {
            //inicjalizacja listy produktów w koszyku
            List<CartVM> cartList = Session["cart"] as List<CartVM>;

            //pobieranie cartVM
            CartVM cartVM = cartList.FirstOrDefault(x => x.ProductId == productId);

            //usuwanie pozycji z koszyka

            cartList.Remove(cartVM);

        }

        public ActionResult PayPalPartial()
        {
            List<CartVM> cartList = Session["cart"] as List<CartVM>;

            return PartialView(cartList);
        }

        [HttpPost]
        public void PlaceOrder()
        {
            List<CartVM> cartList = Session["cart"] as List<CartVM>;

            string userName = User.Identity.Name;

            //deklaracja numeru zamówienia
            int orderId = 0;

            using (Db db = new Db())
            {
                OrderDTO orderDTO = new OrderDTO();

                var user = db.Users.FirstOrDefault(x => x.UserName == userName);
                int userId = user.Id;

                //ustawienie OrderDto i zapis
                orderDTO.UserId = userId;
                orderDTO.CreatedAt = DateTime.Now;

                db.Orders.Add(orderDTO);
                db.SaveChanges();

                //pobranie id zapisanego zamówienia
                orderId = orderDTO.OrderId;

                //inicjalizacja OrderDetailsDTO
                OrderDetailsDTO orderDetailsDTO = new OrderDetailsDTO();

                foreach (var item in cartList)
                {
                    orderDetailsDTO.OrderId = orderId;
                    orderDetailsDTO.UserId = userId;
                    orderDetailsDTO.ProductId = item.ProductId;
                    orderDetailsDTO.Quantity = item.Quantity;

                    db.OrderDetails.Add(orderDetailsDTO);
                    db.SaveChanges();
                }
            }

            //wysyłanie wiadomości email do admina
            var client = new SmtpClient("smtp.mailtrap.io", 2525)
            {
                Credentials = new NetworkCredential("c0b044119273f0", "39c1eab4f64a72"),
                EnableSsl = true
            };
            client.Send("admin@example.com", "admin@example.com", "Nowe Zamówienie", "Masz nowe zamówienie! Numr zamówienia " + orderId);

            //reset sesji
            Session["cart"] = null;
        }

    }
}