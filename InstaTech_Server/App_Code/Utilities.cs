using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using InstaTech.App_Code.Models;

namespace InstaTech.App_Code {
    /// <summary>
    /// Summary description for Utilities
    /// </summary>
    public static class Utilities
    {
        public static string App_Data { get; } = HttpContext.Current.Server.MapPath("~/App_Data/");
        public static string Version { get; } = "1.8.2";

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

        public static void Set_File_Encryption(bool Encrypt)
        {
            var fileList = new List<string>();
            Directory.CreateDirectory(Path.Combine(Utilities.App_Data, "Cases"));
            foreach (var file in Directory.GetFiles(Path.Combine(Utilities.App_Data, "Cases"), "*", SearchOption.AllDirectories))
            {
                fileList.Add(file);
            }
            Directory.CreateDirectory(Path.Combine(Utilities.App_Data, "Tech_Accounts"));
            foreach (var file in Directory.GetFiles(Path.Combine(Utilities.App_Data, "Tech_Accounts"), "*", SearchOption.AllDirectories))
            {
                fileList.Add(file);
            }
            Directory.CreateDirectory(Path.Combine(Utilities.App_Data, "Computer_Accounts"));
            foreach (var file in Directory.GetFiles(Path.Combine(Utilities.App_Data, "Computer_Accounts"), "*", SearchOption.AllDirectories))
            {
                fileList.Add(file);
            }
            foreach (var file in fileList)
            {
                if (Encrypt)
                {
                    File.Encrypt(file);
                }
                else
                {
                    File.Decrypt(file);
                }
            }
        }
        public static void WriteToLog(Exception ex)
        {
            try
            {
                var exception = ex;
                var filePath = Path.Combine(Utilities.App_Data, "Errors", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0') + ".txt");
                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                }
                while (exception != null)
                {
                    var jsonError = new
                    {
                        Type = "Error",
                        Timestamp = DateTime.Now.ToString(),
                        Message = exception?.Message,
                        Source = exception?.Source,
                        StackTrace = exception?.StackTrace,
                        LastMessageType = LastMessageType
                    };
                    File.AppendAllText(filePath, Json.Encode(jsonError) + Environment.NewLine);
                    exception = exception.InnerException;
                }
            }
            catch (Exception ex2)
            {
                // TODO: Report this somehow.
                //HttpContext.Current.Response.Write("There was an error when trying to write to the log files.  Please check the folder and file permissions within App_Data." + Environment.NewLine + Environment.NewLine + "Error: " + Json.Encode(ex2));
            }
        }
        public static void WriteToLog(string Message)
        {
            var filePath = Path.Combine(Utilities.App_Data, "Errors", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0') + ".txt");
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            var jsoninfo = new
            {
                Type = "Info",
                Timestamp = DateTime.Now.ToString(),
                Message = Message
            };
            File.AppendAllText(filePath, Json.Encode(jsoninfo) + Environment.NewLine);
        }
        public static string LastMessageType { get; set; } = "";
    }
}