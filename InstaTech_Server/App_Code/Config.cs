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

        // URL to company logo (relative or absolute).
        public static string Company_Logo_URL { get; } =  "~/Assets/Images/Logo.png";

        // Whether to enable Active Directory tools and features.
        public static bool Active_Directory_Enabled { get; } = true;

        // Major categories of support.  Users will select this from a drop-down list.
        public static List<SelectListItem> Support_Categories { get; } = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "Account/Password", Value = "Account/Password" },
            new SelectListItem() { Text = "Application Slowness/Crashing", Value = "Application Slowness/Crashing" }
        };

        // Sub-categories of support that fall within a major category.
        public static List<Tuple<string, SelectListItem>> Support_Types { get; } = new List<Tuple<string, SelectListItem>>()
        {
            Tuple.Create<string, SelectListItem>("Account/Password", new SelectListItem(){ Text = "Account Locked", Value="Account Locked" }),
            Tuple.Create<string, SelectListItem>("Account/Password", new SelectListItem(){ Text = "Password Reset", Value="Password Reset" })
        };
        
    }
}