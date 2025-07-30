using BrowserStack.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BrowserStack.Helper.DriverManage;
using static BrowserStack.Helper.ExtentManager;
using static BrowserStack.Page.Locator.Login;

namespace BrowserStack.Page.Controller
{
    internal class LoginController : BaseTest
    {
        public void fillName(String name)
        {
            FindElement(userNameField).Clear();
            SendKey(userNameField, name);
        }

        public void fillPassword(String password )
        {
            FindElement(passwordField).Clear();
            SendKey(passwordField, password);
        }

        public void LoginWithoutName()
        {
            fillName("");
            fillPassword("10203040");
            ClickElement(loginBtn);
            NoteReport(status, "Đăng nhập thất bại", "Đăng nhập thiếu tên", "dang_nhap_thieu_ten");
        }

        public void LoginWithoutPassword()
        {
            fillName("bod@example.com");
            fillPassword("");
            ClickElement(loginBtn);
            NoteReport(status, "Đăng nhập thất bại", "Đăng nhập thiếu mật khẩu", "dang_nhap_thieu_mat_khau");
        }

        public void LoginSuccess()
        {
            fillName("bod@example.com");
            fillPassword("10203040");
            ClickElement(loginBtn);
            NoteReport(status, "Đăng nhập thành công", "Đăng nhập thành công", "dang_nhap_thanh_cong");
        }
    }
}
