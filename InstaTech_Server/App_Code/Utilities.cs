using InstaTech.App_Code.Models;
using InstaTech.App_Code.Socket_Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace InstaTech.App_Code {
    /// <summary>
    /// Summary description for Utilities
    /// </summary>
    public static class Utilities
    {
        public static string App_Data { get; } = HttpContext.Current.Server.MapPath("/App_Data/");

        public static List<Case> Open_Cases
        {
            get
            {
                var cases = new List<Case>();
                foreach (Support_Chat sc in Support_Chat.SocketCollection.Where(sc=>(sc as Support_Chat).ConnectionType == Support_Chat.ConnectionTypes.Customer && (sc as Support_Chat).Partner == null))
                {
                    cases.Add(sc.SupportCase);
                }
                if (Config.Demo_Mode)
                {
                    cases.Add(new Case()
                    {
                        CustomerFirstName = "Demo",
                        CustomerLastName = "Customer",
                        CustomerEmail = "demo@instatech.org",
                        CustomerComputerName = "MyFirstPC",
                        CustomerPhone = "555-555-5555",
                        CustomerUserID = "ABCT1000",
                        SupportCategory = "Account Locked/Password Reset",
                        SupportType = "Network Account",
                        Details = "It says my account is locked out and cannot be logged into."
                    });
                }
                return cases;
            }
        }
    }
}