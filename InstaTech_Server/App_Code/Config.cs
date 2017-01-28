using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Web.WebSockets;
using System.Web.Helpers;
using System.Web.WebPages.Html;

namespace InstaTech.App_Code
{
    public class Config
    {
        public static Config Current { get; set; } = new Config();
        // Replace with your company name.
        public string Company_Name { get; } = "InstaTech";

        // This is the single, default admin account that will be created on first start.
        // The temporary password will be "password".  You can use this account to create
        // or modify other tech accounts.
        public string Default_Admin { get; } = "admin";

        // Enable demo accounts.
        public bool Demo_Mode { get; } = true;

        // Whether to enable Active Directory tools and features.
        public bool Active_Directory_Enabled { get; } = false;

        // If Active Directory features are enabled, techs must be in this AD group in order to log in.
        public string Active_Directory_Tech_Group { get; } = "";

        // Determines the default client version of the remote control app that will be downloaded.
        // Values: Windows 7, Windows 8+, Mac, or Linux.
        public string Default_RC_Download { get; } = "Windows 8+";

        // Email settings for sending automated emails (e.g. password resets).
        public string Email_SMTP_Server { get; } = "mail.instatech.org";
        public int Email_SMTP_Port { get; } = 25;
        public string Email_Username { get; } = "DoNotReply@instatech.org";
        public string Email_SMTP_Password { get; } = "";
        // The download paths for each remote control client version.
        public Dictionary<string, string> RC_Download_Paths { get; } = new Dictionary<string, string>()
        {
            { "Windows 8+", "/Downloads/InstaTech Client.exe" },
            { "Windows 7", "/Downloads/InstaTech_CP.exe" },
            { "Mac", "" },
            { "Linux", "/Downloads/InstaTech_CP.AppImage" }
        };

        // Categories of support.  The first value is major category, second is sub-category, and third is the support queue it falls under.
        public List<Tuple<string, string, string>> Support_Categories { get; } = new List<Tuple<string, string, string>>()
        {
            Tuple.Create<string, string, string>("Account Lockout", "Network Account", "General"),
            Tuple.Create<string, string, string>("Account Lockout", "Other", "General"),
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