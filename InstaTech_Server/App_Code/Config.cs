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
        // Replace with your company name.
        public string Company_Name { get; set; } = "InstaTech";


        // This is the single, default admin account that will be created on first start.
        // The temporary password will be "password".  You can use this account to create
        // or modify other tech accounts.
        public string Default_Admin { get; set; } = "admin";

        // Enable demo accounts.
        public bool Demo_Mode { get; set; } = false;

        // Use file encryption for files saved on the server.
        public bool File_Encryption { get; set; } = false;
        // Record the video of remote control sessions.
        public bool Session_Recording { get; set; } = false;

        // Whether to enable Active Directory tools and features.
        public bool Active_Directory_Enabled { get; set; } = false;

        // If Active Directory features are enabled, techs must be in this AD group in order to log in.
        public string Active_Directory_Tech_Group { get; set; } = "";
        // If Active Directory features are enabled, admins must be in this AD group in order to log in.
        public string Active_Directory_Admin_Group { get; set; } = "";

        // Features to enable.  Only the enabled features will appear as buttons on the portal page.
        public bool Feature_Enabled_Chat { get; set; } = true;
        public bool Feature_Enabled_Remote_Control { get; set; } = true;
        public bool Feature_Enabled_Account_Center { get; set; } = true;
        public bool Feature_Enabled_Computer_Hub { get; set; } = true;
        public bool Feature_Enabled_Configuration { get; set; } = true;

        // Email settings for sending automated emails (e.g. password resets).
        public string Email_SMTP_Server { get; set; } = "";
        public int Email_SMTP_Port { get; set; } = 25;
        public string Email_SMTP_Username { get; set; } = "";
        public string Email_SMTP_Password { get; set; } = "";

        // Determines the default client version of the remote control app that will be downloaded.
        // Values: Windows, Windows Service, Mac, or Linux.
        public string Default_RC_Download { get; set; } = "Windows";
        // The download paths for each remote control client version.
        public Dictionary<string, string> RC_Download_Paths { get; set; } = new Dictionary<string, string>()
        {
            { "Windows", "/Downloads/InstaTech_Client.exe" },
            { "Windows Service", "/Downloads/InstaTech_Service.exe" },
            { "Mac", "" },
            { "Linux", "/Downloads/InstaTech_CP.AppImage" }
        };

        // Categories of support.  The first value is major category, second is sub-category, and third is the support queue it falls under.
        public List<Support_Category> Support_Categories { get; set; } = new List<Support_Category>()
        {
            new Support_Category("Account Lockout", "Network Account", "General"),
            new Support_Category("Account Lockout", "Other", "General"),
            new Support_Category("Slowness", "Microsoft Office", "Technical"),
            new Support_Category("Slowness", "Internet", "Technical"),
            new Support_Category("Slowness", "Everything", "Technical"),
            new Support_Category("Slowness", "Other", "Technical"),
            new Support_Category("Crashes", "Microsoft Office", "Technical"),
            new Support_Category("Crashes", "Other", "Technical"),
            new Support_Category("Other", "Other", "Other")
        };

        // Groups that you can use to sort computers and restrict access.  If a tech as a
        // group in their group list, they will be able to interact with all computers
        // within that group.  If a computer is not assigned to a group, only admins will
        // be able to interact with them.
        public List<string> Computer_Groups { get; set; } = new List<string>();

        // Static property to hold the config settings.  Do not alter.
        public static Config Current { get; set; } = new Config();

        // Load config from file.
        public static void Load()
        {
            var savedConfig = Json.Decode<Config>(System.IO.File.ReadAllText(Utilities.App_Data + "Config.json"));
            Current = savedConfig;
        }
        // Save config to file.
        public static void Save()
        {
            System.IO.File.WriteAllText(Utilities.App_Data + "Config.json", Json.Encode(Current));
        }
    }
}