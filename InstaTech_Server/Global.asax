<%@ Application Language="C#" %>

<script runat="server">

    void Application_Start(object sender, EventArgs e)
    {
        try
        {
            InstaTech.App_Code.Utilities.License_Check();
        }
        catch { }
        if (!System.IO.Directory.Exists(InstaTech.App_Code.Utilities.App_Data))
        {
            System.IO.Directory.CreateDirectory(InstaTech.App_Code.Utilities.App_Data);
        }
        // Loads the current config file if it exists.  Otherwise, creates it.  This will make
        // Custom configurations persist through updates, in case the Config.cs file gets changed.
        if (System.IO.File.Exists(InstaTech.App_Code.Utilities.App_Data + "Config.json"))
        {
            InstaTech.App_Code.Config.Load();
        }
        // Resaves the current config.  This will add any new properties introduced to Config.js in updates.
        InstaTech.App_Code.Config.Save();

        // Creates default admin account if it doesn't exist.
        if (!System.IO.Directory.Exists(InstaTech.App_Code.Utilities.App_Data + "Tech_Accounts"))
        {
            System.IO.Directory.CreateDirectory(InstaTech.App_Code.Utilities.App_Data + "Tech_Accounts");
        }
        if (!System.IO.File.Exists(InstaTech.App_Code.Utilities.App_Data + "Tech_Accounts\\" + InstaTech.App_Code.Config.Current.Default_Admin + ".json"))
        {
            var admin = new InstaTech.App_Code.Models.Tech_Account()
            {
                AccessLevel = InstaTech.App_Code.Models.Tech_Account.Access_Levels.Admin,
                TempPassword = "password",
                UserID = InstaTech.App_Code.Config.Current.Default_Admin
            };
            admin.Save();
        }
    }

    void Application_End(object sender, EventArgs e)
    {
        //  Code that runs on application shutdown

    }

    void Application_Error(object sender, EventArgs e)
    {
        // Code that runs when an unhandled error occurs
        var filePath = System.IO.Path.Combine(Server.MapPath("~/App_Data/Errors/"), DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0') + ".txt");
        if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
        }
        var exError = Server.GetLastError();
        while (exError != null)
        {
            var jsonError = new
            {
                Timestamp = DateTime.Now.ToString(),
                Message = exError?.Message,
                InnerEx = exError?.InnerException?.Message,
                Source = exError?.Source,
                StackTrace = exError?.StackTrace,
            };
            var error = System.Web.Helpers.Json.Encode(jsonError) + Environment.NewLine;
            System.IO.File.AppendAllText(filePath, error);
            exError = exError.InnerException;
        }
    }

    void Session_Start(object sender, EventArgs e)
    {
        // Code that runs when a new session is started
        
    }

    void Session_End(object sender, EventArgs e)
    {
        // Code that runs when a session ends. 
        // Note: The Session_End event is raised only when the sessionstate mode
        // is set to InProc in the Web.config file. If session mode is set to StateServer 
        // or SQLServer, the event is not raised.

    }
    void Application_BeginRequest(object sender, EventArgs e)
    {
        if (!Request.IsLocal && !Request.IsSecureConnection && !Request.Url.AbsoluteUri.Contains("test.instatech.org"))
        {
            Response.RedirectPermanent(Request.Url.AbsoluteUri.ToLower().Replace("http://", "https://"), true);
            return;
        }
    }
</script>
