using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Web.WebSockets;
using System.Web.Helpers;
using System.Web.WebPages.Html;

namespace InstaTech.App_Code
{
    public static class Config
    {
        // Replace with your company name.
        public static string Company_Name { get; } = "InstaTech";

        // Enable demo accounts.
        public static bool Demo_Mode { get; } = true;

        // Whether to enable Active Directory tools and features.
        public static bool Active_Directory_Enabled { get; } = false;

        // If Active Directory features are enabled, techs must be in this AD group in order to log in.
        public static string Active_Directory_Tech_Group { get; } = "";

        // Categories of support.  The first value is major category, second is sub-category, and third is the support queue it falls under.
        public static List<Tuple<string, string, string>> Support_Categories { get; } = new List<Tuple<string, string, string>>()
        {
            Tuple.Create<string, string, string>("Account Locked/Password Reset", "Network Account", "General"),
            Tuple.Create<string, string, string>("Account Locked/Password Reset", "Other", "General"),
            Tuple.Create<string, string, string>("Slowness", "Microsoft Office", "Technical"),
            Tuple.Create<string, string, string>("Slowness", "Internet", "Technical"),
            Tuple.Create<string, string, string>("Slowness", "Everything", "Technical"),
            Tuple.Create<string, string, string>("Slowness", "Other", "Technical"),
            Tuple.Create<string, string, string>("Crashes", "Microsoft Office", "Technical"),
            Tuple.Create<string, string, string>("Crashes", "Other", "Technical"),
            Tuple.Create<string, string, string>("Other", "", "Other")
        };
        
    }
}