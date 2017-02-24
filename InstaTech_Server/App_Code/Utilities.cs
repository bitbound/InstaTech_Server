using InstaTech.App_Code.Models;
using InstaTech.App_Code.Socket_Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;

namespace InstaTech.App_Code {
    /// <summary>
    /// Summary description for Utilities
    /// </summary>
    public static class Utilities
    {
        public static string App_Data { get; } = HttpContext.Current.Server.MapPath("~/App_Data/");
        public static string Version { get; } = "1.1.0";
        public static List<Tech_Account> Tech_Accounts
        {
            get
            {
                var accounts = new List<Tech_Account>();
                foreach (var path in Directory.GetFiles(App_Data + "Tech_Accounts"))
                {
                    accounts.Add(Json.Decode<Tech_Account>(File.ReadAllText(path)));
                }
                return accounts;
            }
        }
        public static void License_Check()
        {
            var request = System.Net.WebRequest.CreateHttp("https://instatech.org/Services/Licence_Check.cshtml");
            request.Method = "POST";
            using (var rs = request.GetRequestStream())
            {
                using (var sw = new System.IO.StreamWriter(rs))
                {
                    var content = new
                    {
                        CompanyName = Config.Current.Company_Name,
                        LicenseKey = Config.Current.License_Key
                    };
                    sw.Write(System.Web.Helpers.Json.Encode(content));
                }
            }
            request.GetResponse();
        }
        public static dynamic Clone(object InputObject)
        {
            return Json.Decode(Json.Encode(InputObject));
        }
    }
}