using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserStack.Page.Locator
{
    internal class Cart
    {
        public static By increaseBtn = MobileBy.AccessibilityId("Increase item quantity");
        public static By decreaseBtn = MobileBy.AccessibilityId("Decrease item quantity");
        public static By removeBtn = MobileBy.AccessibilityId("Removes product from cart");
        public static By checkOutBtn = MobileBy.AccessibilityId("Confirms products for checkout");
        public static By quantityProduct = By.Id("com.saucelabs.mydemoapp.android:id/noTV");
        //Shipping Address
        public static By fullname = By.Id("com.saucelabs.mydemoapp.android:id/fullNameET");
        public static By line1 = By.Id("com.saucelabs.mydemoapp.android:id/address1ET");
        public static By city = By.Id("com.saucelabs.mydemoapp.android:id/cityET");
        public static By zipcode = By.Id("com.saucelabs.mydemoapp.android:id/zipET");
        public static By country = By.Id("com.saucelabs.mydemoapp.android:id/countryET");
        public static By payment = By.Id("com.saucelabs.mydemoapp.android:id/paymentBtn");
        //Card infor
        public static By cardName = By.Id("com.saucelabs.mydemoapp.android:id/nameET");
        public static By cardNum = By.Id("com.saucelabs.mydemoapp.android:id/cardNumberET");
        public static By date = By.Id("com.saucelabs.mydemoapp.android:id/expirationDateET");
        public static By securityCode = By.Id("com.saucelabs.mydemoapp.android:id/securityCodeET");
        public static By reviewOrder = By.Id("com.saucelabs.mydemoapp.android:id/paymentBtn");

        public static By placeOrder = By.Id("com.saucelabs.mydemoapp.android:id/paymentBtn");
        public static By alertCheckout = By.Id("com.saucelabs.mydemoapp.android:id/completeTV");
        public static By continueShopping = By.Id("com.saucelabs.mydemoapp.android:id/shoopingBt");
        public static By noItem = By.XPath("//android.widget.TextView[@text=\"Oh no! Your cart is empty. Fill it up with swag to complete your purchase.\"]");
    }
}
