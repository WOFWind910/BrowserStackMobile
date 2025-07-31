using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BrowserStack.Helper.DriverManage;
using static BrowserStack.Helper.ExtentManager;
using static BrowserStack.Page.Locator.Cart;
using BrowserStack.Datatest;
using NUnit.Framework;
using BrowserStack.Tests;
namespace BrowserStack.Page.Controller
{
    internal class CartController : BaseTest
    {
        public void IncreaseQuantity(int times)
        {
            for (int i = 0; i < times; i++)
            {
                ClickElement(increaseBtn);
                Thread.Sleep(1000);
            }
        }

        public void DecreaseQuantity(int times)
        {
            try
            {
                int quantity = int.Parse(FindElement(quantityProduct).Text);
                if (times > quantity) times = quantity;
                for (int i = 0; i < times; i++)
                {
                    ClickElement(decreaseBtn);
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void CheckOut()
        {
            ClickElement(checkOutBtn);
        }

        public void RemoveItem()
        {
            ClickElement(removeBtn);
        }

        public void FillShippingAddress(ShippingAddress s)
        {
            SendKey(fullname, s.fullname);
            SendKey(line1, s.line1);
            SendKey(city, s.city);
            SendKey(zipcode, s.zipcode);
            SendKey(country, s.country);
            NoteReport(status, "Quá trình checkout", "Điền thông tin shipping", "dien_thong_tin_shipping");
            ScrollToElementByResourceId("com.saucelabs.mydemoapp.android:id/paymentBtn");
            ClickElement(payment);
        }

        public void FillCardInfor(CardInfor c)
        {
            SendKey(cardName, c.fullname);
            SendKey(cardNum, c.cardNum);
            SendKey(date, c.date);
            SendKey(securityCode, c.securityCode);
            NoteReport(status, "Quá trình checkout", "Điền thông tin card", "dien_thong_tin_card");
            ClickElement(reviewOrder);
        }

        public void Order()
        {
            ClickElement(placeOrder);
        }

        public void CheckSuccess()
        {
            try
            {
                String alert = FindElement(alertCheckout).Text;
                Assert.AreEqual("Checkout Complete", alert);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            NoteReport(status, "Quá trình checkout", "Checkout thành công", "check_out_thanh_cong");
            ClickElement(continueShopping);
        }

        public void CheckNoItem()
        {
            try
            {
                String alert = FindElement(noItem).Text;
                Assert.AreEqual("Oh no! Your cart is empty. Fill it up with swag to complete your purchase.", alert);
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
