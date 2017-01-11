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
        public static string App_Data { get; } = HttpContext.Current.Server.MapPath("~/App_Data/");

        public static List<Case> Open_Cases
        {
            get
            {
                if (Config.Demo_Mode && Support_Chat.Customers.Where(sc=>sc.Partner == null).Count() == 0)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var newSC = new Support_Chat()
                        {
                            SupportCase = new Case()
                            {
                                CaseID = i,
                                CustomerFirstName = "Demo",
                                CustomerLastName = "Customer " + i,
                                CustomerEmail = "demo@instatech.org",
                                CustomerComputerName = "MyFirstPC",
                                CustomerPhone = "555-555-5555",
                                CustomerUserID = "ABCT1000",
                                SupportCategory = "Account Lockout",
                                SupportType = "Network Account",
                                Details = "It says my account is locked out and cannot be logged into.",
                            },
                            ConnectionType = Support_Chat.ConnectionTypes.Customer,
                        };
                        Support_Chat.SocketCollection.Add(newSC);
                    }
                }
                var cases = new List<Case>();
                foreach (Support_Chat sc in Support_Chat.SocketCollection.Where(sc=>(sc as Support_Chat).ConnectionType == Support_Chat.ConnectionTypes.Customer && (sc as Support_Chat).Partner == null))
                {
                    cases.Add(sc.SupportCase);
                }
                cases.Sort(Comparer<Case>.Create(new Comparison<Case>((Case a, Case b)=> {
                    if (a.DTCreated < b.DTCreated)
                    {
                        return -1;
                    }
                    else if (b.DTCreated < a.DTCreated)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                })));
                return cases;
            }
        }
        private static int compare(Case a, Case b)
        {
            return 1;
        }
    }
}