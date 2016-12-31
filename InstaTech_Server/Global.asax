<%@ Application Language="C#" %>

<script runat="server">

    void Application_Start(object sender, EventArgs e)
    {
        // Code that runs on application startup

    }

    void Application_End(object sender, EventArgs e)
    {
        //  Code that runs on application shutdown

    }

    void Application_Error(object sender, EventArgs e)
    {
        // Code that runs when an unhandled error occurs
        if (!System.IO.Directory.Exists(Server.MapPath("/App_Data/GlobalErrors")))
        {
            System.IO.Directory.CreateDirectory(Server.MapPath("/App_Data/GlobalErrors/"));
        }
        var exError = Server.GetLastError();
        var jsonError = new
        {
            Timestamp = DateTime.Now.ToString(),
            Message = exError.Message,
            InnerEx = exError.InnerException.Message,
            Source = exError.Source,
            StackTrace = exError.StackTrace,
        };
        var error = System.Web.Helpers.Json.Encode(jsonError) + Environment.NewLine;
        System.IO.File.AppendAllText(Server.MapPath("/App_Data/GlobalErrors/" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt"), error);
        throw exError;
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
