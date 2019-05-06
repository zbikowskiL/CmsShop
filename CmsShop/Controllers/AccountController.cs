using CmsShop.Models.Data;
using CmsShop.Models.ViewsModels.Account;
using CmsShop.Models.ViewsModels.Shop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace CmsShop.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        [HttpGet]
        public ActionResult Index()
        {
            return Redirect("~/account/login");
        }

        // GET: Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            //sprawdzenie czy użytkownik jest już zalogowany
            string userName = User.Identity.Name;
            if (!string.IsNullOrEmpty(userName))
                return RedirectToAction("user-profile");

            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public ActionResult Login(LoginUserVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //sprawdzenie użytkownika
            var isValid = false;
            using (Db db = new Db())
            {
                if (db.Users.Any(x => x.UserName.ToLower().Equals(model.UserName.ToLower()) && x.Password.Equals(model.Password)))
                {
                    isValid = true;
                }

                if (!isValid)
                {
                    ModelState.AddModelError("", "Niepoprawna nazwa uzytkownika lub hasło.");
                    return View(model);
                }
                else
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                    return Redirect(FormsAuthentication.GetRedirectUrl(model.UserName, model.RememberMe));
                }
            }
        }

        // GET: /account/create-account
        [ActionName("create-account")]
        [HttpGet]
        public ActionResult CreateAccount()
        {
            return View("CreateAccount");
        }

        // POST: /account/create-account
        [ActionName("create-account")]
        [HttpPost]
        public ActionResult CreateAccount(UserVM model)
        {
            if (!ModelState.IsValid)
            {
                return View("CreateAccount", model);
            }

            if (!model.Password.Equals(model.ConfirmPassword))
            {
                ModelState.AddModelError("", "Podane hasła są niezgodne!");
                return View("CreateAccount", model);
            }

            using (Db db = new Db())
            {
                //sprawdzenie czy nazwa uzytkownika już istnieje
                if (db.Users.Any(x => x.UserName.ToLower().Equals(model.UserName.ToLower())) || db.Users.Any(x => x.EmailAddress.ToLower().Equals(model.EmailAddress.ToLower())))
                {
                    if (db.Users.Any(x => x.UserName.Equals(model.UserName)))
                    {
                        ModelState.AddModelError("", $"Użytkownik o nazwie {model.UserName} już istnieje!");
                        model.UserName = "";
                        return View("CreateAccount", model);
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Podany adres email: {model.UserName} już istnieje!");
                        model.EmailAddress = "";
                        return View("CreateAccount", model);
                    }

                }

                //utworzenie użytkownika
                UserDTO userDTO = new UserDTO()
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailAddress = model.EmailAddress,
                    UserName = model.UserName,
                    Password = model.Password
                };

                db.Users.Add(userDTO);
                db.SaveChanges();

                //dodanie roli dla użytkownika
                UserRoleDTO userRole = new UserRoleDTO()
                {
                    UserId = userDTO.Id,
                    RoleId = 2
                };

                db.UserRoles.Add(userRole);
                db.SaveChanges();

            }

            //Komunikat o zalogowaniu
            TempData["SM"] = "Jesteś zarejestrowany! Możesz się zalogować.";

            return Redirect("~/account/login");
        }

        // GET: /account/logout
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            return Redirect("~/account/login");
        }

        [Authorize]
        public ActionResult UserNavPartial()
        {
            string userName = User.Identity.Name;

            //deklaracja modelu
            UserNavPartialVM model;

            using (Db db = new Db())
            {
                UserDTO dto = db.Users.FirstOrDefault(x => x.UserName == userName);

                model = new UserNavPartialVM()
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName
                };
            }

            return PartialView(model);
        }

        // GET: /account/user-profile
        [Authorize]
        [ActionName("user-profile")]
        public ActionResult UserProfile()
        {
            string userName = User.Identity.Name;

            UserProfileVM model;

            using (Db db = new Db())
            {
                UserDTO dto = db.Users.FirstOrDefault(x => x.UserName == userName);

                model = new UserProfileVM(dto);
            }

            return View("UserProfile", model);
        }

        // POST: /account/user-profile
        [ActionName("user-profile")]
        [HttpPost]
        [Authorize]
        public ActionResult UserProfile(UserProfileVM model)
        {
            if (!ModelState.IsValid)
            {
                return View("UserProfile", model);
            }

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                if (!model.Password.Equals(model.ConfirmPassword))
                {
                    ModelState.AddModelError("", "Hasła nie pasują do siebie");
                    return View("UserProfile", model);
                }
            }

            using (Db db = new Db())
            {
                string userName = User.Identity.Name;

                if (db.Users.Where(x => x.Id != model.Id).Any(x => x.UserName == userName))
                {
                    ModelState.AddModelError("", $"Nazwa użytkowniak {model.UserName} jest już zajęta.");
                    model.UserName = "";
                    return View("UserProfile", model);
                }

                if (db.Users.Where(x => x.Id != model.Id).Any(x => x.EmailAddress == model.EmailAddress))
                {
                    ModelState.AddModelError("", $"Istnieje już użytkownik o podanym adresie email: {model.EmailAddress}.");
                    return View("UserProfile", model);
                }

                UserDTO dto = db.Users.Find(model.Id);
                dto.FirstName = model.FirstName;
                dto.LastName = model.LastName;
                dto.EmailAddress = model.EmailAddress;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    dto.Password = model.Password;
                }

                db.SaveChanges();

            }

            TempData["SM"] = "Wyedytowałeś swój profil!";

            return Redirect("~/account/user-profile");
        }

        // GET: /account/orders
        [Authorize(Roles = "User")]
        public ActionResult Orders()
        {
            //inicjalizacja listy zamówień uzytkownika
            List<OrdersForUserVM> ordersForUsers = new List<OrdersForUserVM>();

            using (Db db = new Db())
            {
                UserDTO user = db.Users.Where(x => x.UserName == User.Identity.Name).FirstOrDefault();
                int userId = user.Id;

                //pobieranie zamowienia dla użytkownika
                List<OrderVM> orders = db.Orders.Where(x => x.UserId == userId).ToArray().Select(x => new OrderVM(x)).ToList();

                foreach (var order in orders)
                {
                    //inicjalizacja słownika produktów
                    Dictionary<string, int> productsAndQuantity = new Dictionary<string, int>();
                    decimal total = 0m;

                    //pobieranie szegółów zamówienia
                    List<OrderDetailsDTO> orderDetailsDTO = db.OrderDetails.Where(x => x.OrderId == order.OrderId).ToList();

                    foreach (var orderDetails in orderDetailsDTO)
                    {
                        //pobranie produktu
                        ProductDTO product = db.Products.Where(x => x.Id == orderDetails.ProductId).FirstOrDefault();

                        //pobranie ceny produktu
                        decimal price = product.Price;

                        //pobranie nazwy produktu
                        string productName = product.Name;

                        //dodanie produktu do słownika
                        productsAndQuantity.Add(productName, orderDetails.Quantity);

                        total += orderDetails.Quantity * price;
                    }

                    ordersForUsers.Add(new OrdersForUserVM()
                    {
                        OrderNumber = order.OrderId,
                        Total = total,
                        ProductsAndQuantity = productsAndQuantity,
                        CreatedAt = order.CreatedAt
                    });
                }
            }
            return View(ordersForUsers);
        }

    }
}