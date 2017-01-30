<%@ Application Language="C#" %>

<script runat="server">

    void Application_Start(object sender, EventArgs e)
    {
        if (!System.IO.Directory.Exists(InstaTech.App_Code.Utilities.App_Data))
        {
            System.IO.Directory.CreateDirectory(InstaTech.App_Code.Utilities.App_Data);
        }
        // Loads the current config file if it exists.  Otherwise, creates it.  This will make
        // Custom configurations persist through updates, in case the Config.cs file gets changed.
        if (System.IO.File.Exists(InstaTech.App_Code.Utilities.App_Data + "Config.json"))
        {
            InstaTech.App_Code.Config savedConfig = System.Web.Helpers.Json.Decode<InstaTech.App_Code.Config>(System.IO.File.ReadAllText(InstaTech.App_Code.Utilities.App_Data + "Config.json"));
            InstaTech.App_Code.Config.Current = savedConfig;
        }
        else
        {
            System.IO.File.WriteAllText(InstaTech.App_Code.Utilities.App_Data + "Config.json", System.Web.Helpers.Json.Encode(InstaTech.App_Code.Config.Current));
        }
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
        if (!System.IO.Directory.Exists(Server.MapPath("~/App_Data/Errors")))
        {
            System.IO.Directory.CreateDirectory(Server.MapPath("~/App_Data/Errors/"));
        }
        var exError = Server.GetLastError();
        var jsonError = new
        {
            Timestamp = DateTime.Now.ToString(),
            Message = exError?.Message,
            InnerEx = exError?.InnerException?.Message,
            Source = exError?.Source,
            StackTrace = exError?.StackTrace,
        };
        var error = System.Web.Helpers.Json.Encode(jsonError) + Environment.NewLine;
        System.IO.File.AppendAllText(Server.MapPath("~/App_Data/Errors/" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt"), error);
    }

    void Session_Start(object sender, EventArgs e)
    {
        // Code that runs when a new session is started
        if (!Request.IsLocal && !Request.IsSecureConnection)
        {
            Response.RedirectPermanent(Request.Url.AbsoluteUri.ToLower().Replace("http://", "https://"), true);
            return;
        }

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

    }
</script>
