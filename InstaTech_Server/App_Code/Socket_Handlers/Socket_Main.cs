using InstaTech.App_Code.Models;
using Microsoft.Web.WebSockets;
using System;
using System.Collections.Generic;
using System.IdentityModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Helpers;

namespace InstaTech.App_Code.Socket_Handlers
{
    public class Socket_Main : WebSocketHandler
    {
        #region User-Defined Properties/Fields
        public static WebSocketCollection SocketCollection { get; } = new WebSocketCollection();
        public static List<Socket_Main> Customers
        {
            get
            {
                return SocketCollection.Cast<Socket_Main>().ToList().FindAll(sc => sc.ConnectionType == ConnectionTypes.Customer);
            }
        }
        public static List<Socket_Main> AvailableTechs
        {
            get
            {
                return SocketCollection.Cast<Socket_Main>().ToList().FindAll(sc => sc.ConnectionType == ConnectionTypes.Technician && sc.LoggedIntoChat == true);
            }
        }
        public Socket_Main Partner { get; set; }
        public Case SupportCase { get; set; }
        public Tech_Account TechAccount { get; set; }
        public bool LoggedIntoChat { get; set; }
        public string AuthenticationToken { get; set; }
        public int BadLoginAttempts { get; set; } = 0;
        public ConnectionTypes? ConnectionType { get; set; }
        public enum ConnectionTypes
        {
            Customer,
            Technician
        }
        #endregion

        public Socket_Main()
        {
            this.MaxIncomingMessageSize = 999999999;
        }

        #region Socket Events
        public override void OnOpen()
        {
            SocketCollection.Add(this);
        }
        public override void OnMessage(byte[] message)
        {
            if (Partner != null)
            {
                Partner.Send(message);
            }
        }
        public override void OnMessage(string message)
        {
            dynamic jsonMessage = Json.Decode(message);
            
            if (jsonMessage == null || String.IsNullOrEmpty(jsonMessage?.Type))
            {
                throw new Exception("Type is null within Socket_Main.OnMessage.");
            }
            var methodHandler = Type.GetType("InstaTech.App_Code.Socket_Handlers.Socket_Main").GetMethods().FirstOrDefault(mi => mi.Name == "Handle" + jsonMessage.Type);
            if (methodHandler != null)
            {
                try
                {
                    methodHandler.Invoke(this, new object[] { jsonMessage });
                }
                catch (Exception ex)
                {
                    if (!Directory.Exists(HttpContext.Current.Server.MapPath("~/App_Data/Errors")))
                    {
                        Directory.CreateDirectory(HttpContext.Current.Server.MapPath("~/App_Data/Errors/"));
                    }
                    var jsonError = new
                    {
                        Timestamp = DateTime.Now.ToString(),
                        Message = ex.Message,
                        InnerEx = ex.InnerException.Message,
                        Source = ex.Source,
                        StackTrace = ex.StackTrace,
                    };
                    var error = Json.Encode(jsonError) + Environment.NewLine;
                    File.AppendAllText(HttpContext.Current.Server.MapPath("~/App_Data/Errors/" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt"), error);
                    throw ex;
                }
            }
        }
        public override void OnClose()
        {
            SocketCollection.Remove(this);
            if (Partner != null)
            {
                var request = new
                {
                    Type = "LostPartner"
                };
                Partner.Send(Json.Encode(request));
            }
            if (ConnectionType == ConnectionTypes.Technician && Partner != null)
            {
                Partner.SupportCase.TechUserID = null;
                var strUpdate = Json.Encode(new
                {
                    Type = "CaseUpdate",
                    Status = "Add",
                    Case = Partner.SupportCase
                });
                AvailableTechs.ForEach((Socket_Main sc) =>
                {
                    sc.Send(strUpdate);
                });
                Partner.Partner = null;
                Partner = null;
            }
            if (ConnectionType == ConnectionTypes.Customer)
            {
                foreach (Socket_Main socket in Customers.Where(cu => cu.Partner == null))
                {
                    socket.SendWaitUpdate();
                }
                var request = Json.Encode(new
                {
                    Type = "CaseUpdate",
                    Status = "Remove",
                    Case = SupportCase
                });
                AvailableTechs.ForEach((Socket_Main sc) => {
                    sc.Send(request);
                });
                if (SupportCase.Status == Case.CaseStatus.Open)
                {
                    SupportCase.Status = Case.CaseStatus.Abandoned;
                }
                SupportCase.Save();
            }
        }
        public override void OnError()
        {
            SocketCollection.Remove(this);
            Directory.CreateDirectory(Utilities.App_Data + @"/WebSocket_Errors/");
            File.WriteAllText(Utilities.App_Data + @"/WebSocket_Errors/" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt", Json.Encode(Error) + Environment.NewLine);
            if (Partner != null)
            {
                var request = new
                {
                    Type = "LostPartner"
                };
                Partner.Send(Json.Encode(request));
            }
            if (ConnectionType == ConnectionTypes.Technician && Partner != null)
            {
                SupportCase.TechUserID = null;
                var strUpdate = Json.Encode(new
                {
                    Type = "CaseUpdate",
                    Status = "Add",
                    Case = SupportCase
                });
                AvailableTechs.ForEach((Socket_Main sc) =>
                {
                    sc.Send(strUpdate);
                });
                Partner.Partner = null;
                Partner = null;
            }
            if (ConnectionType == ConnectionTypes.Customer)
            {
                foreach (Socket_Main socket in Customers.Where(cu => cu.Partner == null))
                {
                    socket.SendWaitUpdate();
                }
                var strRequest = Json.Encode(new
                {
                    Type = "CaseUpdate",
                    Status = "Remove",
                    Case = SupportCase
                });
                AvailableTechs.ForEach((Socket_Main sc) => {
                    sc.Send(strRequest);
                });
                if (SupportCase.Status == Case.CaseStatus.Open)
                {
                    SupportCase.Status = Case.CaseStatus.Abandoned;
                }
                SupportCase.Save();
            }
        }
        #endregion

        #region Helper Methods
        private int GetPlaceInQueue()
        {
            var cases = SocketCollection.Where(sock => (sock as Socket_Main).ConnectionType == ConnectionTypes.Customer && (sock as Socket_Main).Partner == null && (sock as Socket_Main)?.SupportCase?.DTCreated < SupportCase?.DTCreated);
            return cases.Count() + 1;
        }
        private bool LoginTech(dynamic JsonData)
        {
            if (ConnectionType != null && ConnectionType != ConnectionTypes.Technician)
            {
                return false;
            }
            if (BadLoginAttempts >= 3)
            {
                JsonData.Status = "temp ban";
                Send(Json.Encode(JsonData));
                return false;
            }
            if (Config.Current.Demo_Mode && JsonData.UserID.ToLower() == "demo" && JsonData.Password == "tech")
            {
                ConnectionType = ConnectionTypes.Technician;
                AuthenticationToken = Guid.NewGuid().ToString().Replace("-", "");
                TechAccount = new Tech_Account()
                {
                    UserID = "demo",
                    FirstName = "Demo",
                    LastName = "Tech",
                    HashedPassword = Crypto.HashPassword(JsonData.Password)
                };
                if (JsonData.RememberMe == true)
                {
                    TechAccount.AuthenticationToken = AuthenticationToken;
                }
                else
                {
                    TechAccount.AuthenticationToken = null;
                }
                TechAccount.Save();
                JsonData.Status = "ok";
                JsonData.AuthenticationToken = AuthenticationToken;
                Send(Json.Encode(JsonData));
                return true;
            }
            else if (Config.Current.Active_Directory_Enabled)
            {
                // TODO: AD authentication.
                return false;
            }
            else
            {
                if (!Directory.Exists(Utilities.App_Data + "Tech_Accounts"))
                {
                    Directory.CreateDirectory(Utilities.App_Data + "Tech_Accounts");
                }
                if (!File.Exists(Utilities.App_Data + "Tech_Accounts\\" + JsonData.UserID + ".json"))
                {
                    BadLoginAttempts++;
                    JsonData.Status = "invalid";
                    Send(Json.Encode(JsonData));
                    return false;
                }
                Tech_Account account = Json.Decode<Tech_Account>(File.ReadAllText(Utilities.App_Data + "Tech_Accounts\\" + JsonData.UserID + ".json"));
                if (account.BadLoginAttempts >= 3)
                {
                    if (DateTime.Now - account.LastBadLogin > TimeSpan.FromMinutes(10))
                    {
                        BadLoginAttempts = 0;
                    }
                    else
                    {
                        JsonData.Status = "locked";
                        Send(Json.Encode(JsonData));
                        return false;
                    }
                }
                if (JsonData.Password == account.TempPassword)
                {
                    if (String.IsNullOrEmpty(JsonData.NewPassword))
                    {
                        JsonData.Status = "new required";
                        Send(Json.Encode(JsonData));
                        return false;
                    }
                    else if (JsonData.NewPassword != JsonData.ConfirmNewPassword)
                    {
                        JsonData.Status = "password mismatch";
                        Send(Json.Encode(JsonData));
                        return false;
                    }
                    else if (JsonData.NewPassword.Length < 8 || JsonData.NewPassword.Length > 20)
                    {
                        JsonData.Status = "password length";
                        Send(Json.Encode(JsonData));
                        return false;
                    }
                    else
                    {
                        ConnectionType = ConnectionTypes.Technician;
                        AuthenticationToken = Guid.NewGuid().ToString().Replace("-", "");
                        account.TempPassword = null;
                        account.HashedPassword = Crypto.HashPassword(JsonData.ConfirmNewPassword);
                        account.BadLoginAttempts = 0;
                        if (JsonData.RememberMe == true)
                        {
                            account.AuthenticationToken = AuthenticationToken;
                        }
                        else
                        {
                            account.AuthenticationToken = null;
                        }
                        account.Save();
                        JsonData.Status = "ok";
                        JsonData.AuthenticationToken = AuthenticationToken;
                        Send(Json.Encode(JsonData));
                        return true;
                    }
                }
                if (Crypto.VerifyHashedPassword(account.HashedPassword, JsonData.Password))
                {
                    ConnectionType = ConnectionTypes.Technician;
                    AuthenticationToken = Guid.NewGuid().ToString().Replace("-", "");
                    account.BadLoginAttempts = 0;
                    account.TempPassword = null;
                    if (JsonData.RememberMe == true)
                    {
                        account.AuthenticationToken = AuthenticationToken;
                    }
                    else
                    {
                        account.AuthenticationToken = null;
                    }
                    account.Save();
                    TechAccount = account;
                    JsonData.Status = "ok";
                    JsonData.AuthenticationToken = AuthenticationToken;
                    Send(Json.Encode(JsonData));
                    return true;
                }
                if (!String.IsNullOrEmpty(JsonData.AuthenticationToken))
                {
                    if (JsonData.AuthenticationToken == AuthenticationToken || JsonData.AuthenticationToken == account.AuthenticationToken)
                    {
                        ConnectionType = ConnectionTypes.Technician;
                        AuthenticationToken = JsonData.AuthenticationToken;
                        account.BadLoginAttempts = 0;
                        account.TempPassword = null;
                        account.Save();
                        TechAccount = account;
                        JsonData.Status = "ok";
                        Send(Json.Encode(JsonData));
                        return true;
                    }
                    else
                    {
                        BadLoginAttempts++;
                        JsonData.Status = "invalid";
                        Send(Json.Encode(JsonData));
                        return false;
                    }
                }
                // Bad login attempt.
                BadLoginAttempts++;
                account.BadLoginAttempts++;
                account.LastBadLogin = DateTime.Now;
                account.Save();
                JsonData.Status = "invalid";
                Send(Json.Encode(JsonData));
                return false;
            }
        }
        private bool AuthenticateTech(dynamic JsonData)
        {
            if (JsonData.AuthenticationToken != AuthenticationToken)
            {
                var response = new
                {
                    Type = "SessionEnded",
                    Details = "An unauthorized request was made."
                };
                Send(Json.Encode(response));
                Close();
                return false;
            }
            else
            {
                return true;
            }
        }
        public void SendWaitUpdate()
        {
            var request = new
            {
                Type = "WaitUpdate",
                Place = GetPlaceInQueue(),
            };
            Send(Json.Encode(request));
        }
        #endregion

        #region Socket Message Handlers - Shared
        public void HandleSessionEnded(dynamic JsonData)
        {
            if (ConnectionType == ConnectionTypes.Technician)
            {
                SupportCase.DTClosed = DateTime.Now;
                SupportCase.Status = Case.CaseStatus.Resolved;
                SupportCase.Save();
                Partner.Send(Json.Encode(JsonData));
                Partner.Close();
            }
            else if (ConnectionType == ConnectionTypes.Customer)
            {
                SupportCase.DTClosed = DateTime.Now;
                SupportCase.Status = Case.CaseStatus.Abandoned;
                SupportCase.Save();
                Close();
            }
        }
        public void HandleTechMainLogin(dynamic JsonData)
        {
            LoginTech(JsonData);
        }
        #endregion

        #region Socket Message Handlers - Chat
        public void HandleCustomerChatLogin(dynamic JsonData)
        {
            // Prevent socket from somehow logging in twice.
            if (ConnectionType != null)
            {
                return;
            }
            SupportCase = new Case()
            {
                CustomerFirstName = JsonData.FirstName,
                CustomerLastName = JsonData.LastName,
                CustomerUserID = JsonData.UserID,
                CustomerComputerName = JsonData.ComputerName,
                CustomerPhone = JsonData.Phone,
                CustomerEmail = JsonData.Email,
                SupportCategory = JsonData.SupportCategory,
                SupportType = JsonData.SupportType,
                Details = JsonData.Details
            };
            ConnectionType = ConnectionTypes.Customer;
            SupportCase.Save();
            JsonData.Status = "ok";
            JsonData.Place = GetPlaceInQueue();
            Send(Json.Encode(JsonData));
            var request = Json.Encode(new
            {
                Type = "CaseUpdate",
                Status = "Add",
                Case = SupportCase
            });
            AvailableTechs.ForEach((Socket_Main sc) => {
                sc.Send(request);
            });
            if (Config.Current.Demo_Mode && AvailableTechs.Count == 0)
            {
                TechBot.Notify(this);
            }
        }
        public void HandleGetSupportCategories(dynamic JsonData)
        {
            var categories = new List<string>();
            foreach (var tuple in Config.Current.Support_Categories)
            {
                categories.Add(tuple.Item1);
            }
            JsonData.Categories = categories.Distinct();
            Send(Json.Encode(JsonData));
        }
        public void HandleGetCustomerFormInfo(dynamic JsonData)
        {
            try
            {
                var query = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
                // TODO: Test in AD environment.
                //ConnectionOptions conn = new ConnectionOptions();
                //conn.Username = "";
                //conn.Password = "";
                //conn.Authority = "ntlmdomain:DOMAIN";
                //var scope = new ManagementScope(String.Format("\\\\{0}\\root\\CIMV2", WebSocketContext.UserHostName), conn);
                //ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(String.Format("\\\\{0}\\root\\CIMV2", WebSocketContext.UserHostName ?? WebSocketContext.UserHostAddress), "SELECT * FROM Win32_ComputerSystem");
                ManagementObjectCollection collection = searcher.Get();
                foreach (ManagementObject mo in collection)
                {
                    foreach (var moProp in mo.Properties)
                    {
                        JsonData[moProp.Name] = moProp?.Value?.ToString();
                    }
                }
            }
            catch { }
            Send(Json.Encode(JsonData));
        }
        public void HandleGetSupportTypes(dynamic JsonData)
        {
            var tuples = Config.Current.Support_Categories.FindAll(tp => tp.Item1 == JsonData.SupportCategory);
            var types = new List<string>();
            foreach (var tuple in tuples)
            {
                types.Add(tuple.Item2);
            }
            JsonData.Types = types;
            Send(Json.Encode(JsonData));
        }
        public void HandleTechChatLogin(dynamic JsonData)
        {
            if (LoginTech(JsonData))
            {
                LoggedIntoChat = true;
            }
        }
        public void HandleExitTechChat(dynamic JsonData)
        {
            LoggedIntoChat = false;
        }
        public void HandleGetQueues(dynamic JsonData)
        {
            if (!AuthenticateTech(JsonData))
            {
                return;
            }
            var queues = new List<string>();
            foreach (var tuple in Config.Current.Support_Categories)
            {
                queues.Add(tuple.Item3);
            }
            JsonData.Queues = queues.Distinct();
            Send(Json.Encode(JsonData));
        }
        public void HandleGetCases(dynamic JsonData)
        {
            if (!AuthenticateTech(JsonData))
            {
                return;
            }
            JsonData.Cases = Utilities.Open_Cases;
            Send(Json.Encode(JsonData));
        }
        public void HandleTakeCase(dynamic JsonData)
        {
            if (!AuthenticateTech(JsonData))
            {
                return;
            }
            var takeCase = Utilities.Open_Cases.Find(cs => cs.CaseID == int.Parse(JsonData.CaseID));
            if (takeCase == null)
            {
                JsonData.Status = "taken";
                Send(Json.Encode(JsonData));
                return;
            }
            if (takeCase.TechUserID != null)
            {
                JsonData.Status = "taken";
                Send(Json.Encode(JsonData));
                return;
            }
            if (takeCase.Locked && takeCase.LockedBy != TechAccount.UserID)
            {
                JsonData.Status = "locked";
                Send(Json.Encode(JsonData));
                return;
            }

            if (Partner != null)
            {
                Partner.Send(Json.Encode(new { Type = "LostPartner" }));
                Partner.SupportCase.TechUserID = null;
                Partner.Partner = null;
            }
            takeCase.TechUserID = TechAccount.UserID;
            takeCase.DTReceived = DateTime.Now;
            Partner = SocketCollection.FirstOrDefault(sc => (sc as Socket_Main)?.SupportCase?.CaseID == takeCase.CaseID) as Socket_Main;
            SupportCase = Partner.SupportCase;
            Partner.Partner = this;
            takeCase.Save();
            TechAccount.Cases.Add(takeCase.CaseID);
            TechAccount.Save();
            JsonData.Status = "ok";
            JsonData.TechID = TechAccount.UserID;
            JsonData.TechFirstName = TechAccount.FirstName;
            JsonData.TechLastName = TechAccount.LastName;
            Partner.Send(Json.Encode(JsonData));
            JsonData.PreviousMessages = SupportCase.Messages;
            Send(Json.Encode(JsonData));
            foreach (Socket_Main socket in Customers.Where(cu => cu.Partner == null))
            {
                socket.SendWaitUpdate();
            }
            var request = Json.Encode(new
            {
                Type = "CaseUpdate",
                Status = "Remove",
                Case = SupportCase
            });
            AvailableTechs.ForEach((Socket_Main sc) => {
                sc.Send(request);
            });
        }
        public void HandleChatMessage(dynamic JsonData)
        {
            if (ConnectionType == ConnectionTypes.Technician && !AuthenticateTech(JsonData))
            {
                return;
            }
            var message = new ChatMessage();
            message.DTSent = DateTime.Now;
            if (ConnectionType == ConnectionTypes.Technician)
            {
                message.FromUserID = TechAccount.UserID;
            }
            else
            {
                message.FromUserID = SupportCase.CustomerUserID;
            }
            message.Content = Encoding.UTF8.GetString(Convert.FromBase64String(JsonData.Message));
            SupportCase.Messages.Add(message);
            SupportCase.Save();
            if (Partner != null)
            {
                Partner.Send(Json.Encode(JsonData));
            }
            if (Config.Current.Demo_Mode && Partner.WebSocketContext == null)
            {
                var response = new
                {
                    Type = "ChatMessage",
                    Message = Convert.ToBase64String(Encoding.UTF8.GetBytes("Hi.  I'm just a bot.  I haven't been programmed with any cool responses yet.")),
                };
                for (int i = 0; i < 10; i++)
                {
                    Send(Json.Encode(new { Type = "Typing" }));
                    Thread.Sleep(500);
                }
                Send(Json.Encode(response));
            }
        }
        public void HandleTyping(dynamic JsonData)
        {
            if (ConnectionType == ConnectionTypes.Technician && !AuthenticateTech(JsonData))
            {
                return;
            }
            if (Partner != null)
            {
                Partner.Send(Json.Encode(JsonData));
            }
        }
        public void HandleForgotPassword(dynamic JsonData)
        {
            if (!File.Exists(Utilities.App_Data + "Tech_Accounts\\" + JsonData.UserID + ".json"))
            {
                JsonData.Status = "invalid";
                Send(Json.Encode(JsonData));
                return;
            }
            Tech_Account account = Json.Decode<Tech_Account>(File.ReadAllText(Utilities.App_Data + "Tech_Accounts\\" + JsonData.UserID + ".json"));
            if (account.Email == null)
            {
                JsonData.Status = "noemail";
                Send(Json.Encode(JsonData));
                return;
            }
            account.TempPassword = Path.GetRandomFileName().Replace(".", "");
            account.Save();
            JsonData.Status = "ok";
            JsonData.TempPassword = account.TempPassword;
            try
            {
                WebMail.SmtpServer = Config.Current.Email_SMTP_Server;
                WebMail.UserName = Config.Current.Email_Username;
                WebMail.Password = Config.Current.Email_SMTP_Password;
                WebMail.From = Config.Current.Email_Username;
                WebMail.Send(account.Email, Config.Current.Company_Name + " Support Portal Password Reset", "As requested, your password has been reset.  Your temporary password is below.<br><br>If you did not request this password reset, or requested it in error, you can safely ignore this email.  Logging in with your old password will invalidate the temporary password and reverse the password reset.<br><br>Temporary Password: " + account.TempPassword);
                Send(Json.Encode(JsonData));
            }
            catch
            {
                JsonData.Status = "error";
                Send(Json.Encode(JsonData));
            }
        }
        public void HandleSendToQueue(dynamic JsonData)
        {
            Partner.Send(Json.Encode(new { Type = "SendToQueue" }));
            var strUpdate = Json.Encode(new
            {
                Type = "CaseUpdate",
                Status = "Add",
                Case = SupportCase
            });
            AvailableTechs.ForEach((Socket_Main sc) =>
            {
                sc.Send(strUpdate);
            });
            SupportCase.TechUserID = null;
            Partner.Partner = null;
            Partner = null;
        }
        public void HandleCaseUpdate(dynamic JsonData)
        {
            if (JsonData.Status == "Transfer")
            {
                var transferCase = Utilities.Open_Cases.Find(ca => ca.CaseID == int.Parse(JsonData.CaseID));
                if (transferCase != null)
                {
                    if (transferCase.TechUserID != null)
                    {
                        return;
                    }
                    if (JsonData.SupportCategory != "Other")
                    {
                        if (String.IsNullOrWhiteSpace(JsonData.SupportCategory) || String.IsNullOrWhiteSpace(JsonData.SupportType))
                        {
                            return;
                        }
                    }
                    transferCase.SupportCategory = JsonData.SupportCategory;
                    if (transferCase.SupportCategory == "Other")
                    {
                        transferCase.SupportType = "";
                    }
                    else
                    {
                        transferCase.SupportType = JsonData.SupportType;
                    }
                    transferCase.Save();
                    var request = Json.Encode(new
                    {
                        Type = "CaseUpdate",
                        Status = "Remove",
                        Case = transferCase
                    });
                    AvailableTechs.ForEach((Socket_Main sc) =>
                    {
                        sc.Send(request);
                    });
                    request = Json.Encode(new
                    {
                        Type = "CaseUpdate",
                        Status = "Add",
                        Case = transferCase
                    });
                    AvailableTechs.ForEach((Socket_Main sc) =>
                    {
                        sc.Send(request);
                    });
                }
            }
        }
        public void HandleLockCase(dynamic JsonData)
        {
            var lockCase = Utilities.Open_Cases.Find(ca => ca.CaseID == int.Parse(JsonData.CaseID));
            if (lockCase == null)
            {
                JsonData.Status = "taken";
                Send(Json.Encode(JsonData));
                return;
            }
            if (lockCase.TechUserID != null)
            {
                JsonData.Status = "taken";
                Send(Json.Encode(JsonData));
                return;
            }
            if (lockCase.Locked == true)
            {
                JsonData.Status = "already locked";
                Send(Json.Encode(JsonData));
                return;
            }
            lockCase.Locked = true;
            lockCase.LockedBy = TechAccount.UserID;
            lockCase.Save();
            new Timer((object lockedCase) => {
                (lockedCase as Case).Locked = false;
                (lockedCase as Case).LockedBy = null;
                (lockedCase as Case).Save();
                var request = new
                {
                    Type = "UnlockCase",
                    CaseID = (lockedCase as Case).CaseID
                };
                AvailableTechs.ForEach((sc) =>
                {
                    sc.Send(Json.Encode(request));
                });
            }, lockCase, 20000, Timeout.Infinite);
            JsonData.Status = "ok";
            AvailableTechs.ForEach((sc) =>
            {
                sc.Send(Json.Encode(JsonData));
            });
        }
        #endregion
    }
}
