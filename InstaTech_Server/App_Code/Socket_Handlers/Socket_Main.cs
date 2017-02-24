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
        public static List<Case> Open_Cases
        {
            get
            {
                if (Config.Current.Demo_Mode && Socket_Main.Customers?.Where(sc => sc.Partner == null)?.Count() == 0)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var newSC = new Socket_Main()
                        {
                            SupportCase = new Case()
                            {
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
                            ConnectionType = Socket_Main.ConnectionTypes.Customer,
                        };
                        Socket_Main.SocketCollection.Add(newSC);
                    }
                }
                var cases = new List<Case>();
                foreach (Socket_Main sc in Socket_Main.SocketCollection.Where(sc => (sc as Socket_Main).ConnectionType == Socket_Main.ConnectionTypes.Customer && (sc as Socket_Main).Partner == null))
                {
                    cases.Add(sc.SupportCase);
                }
                cases.Sort(Comparer<Case>.Create(new Comparison<Case>((Case a, Case b) => {
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
                    var filePath = Path.Combine(Utilities.App_Data, "Errors", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0') + ".txt");
                    if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    }
                    var jsonError = new
                    {
                        Timestamp = DateTime.Now.ToString(),
                        Message = ex?.Message,
                        Source = ex?.Source,
                        StackTrace = ex?.StackTrace,
                    };
                    var error = Json.Encode(jsonError) + Environment.NewLine;
                    File.AppendAllText(filePath, error);
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
            var filePath = Path.Combine(Utilities.App_Data, "WebSocket_Errors", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0') + ".txt");
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            var jsonError = new
            {
                Timestamp = DateTime.Now.ToString(),
                Message = Error?.Message,
                InnerEx = Error?.InnerException?.Message,
                Source = Error?.Source,
                StackTrace = Error?.StackTrace,
            };
            var error = Json.Encode(jsonError) + Environment.NewLine;
            File.AppendAllText(filePath, error);

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
        private bool AuthenticateAdmin (dynamic JsonData)
        {
            if (JsonData.AuthenticationToken != AuthenticationToken || TechAccount.AccessLevel != Tech_Account.Access_Levels.Admin)
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
            if (ConnectionType != null && ConnectionType != ConnectionTypes.Technician)
            {
                return;
            }
            if (BadLoginAttempts >= 3)
            {
                JsonData.Status = "temp ban";
                Send(Json.Encode(JsonData));
                return;
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
                    HashedPassword = Crypto.HashPassword(JsonData.Password),
                    AccessLevel = Tech_Account.Access_Levels.Admin
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
                JsonData.Access = TechAccount.AccessLevel.ToString();
                JsonData.AuthenticationToken = AuthenticationToken;
                Send(Json.Encode(JsonData));
                return;
            }
            //else if (Config.Current.Active_Directory_Enabled)
            //{
            //    // TODO: AD authentication.
            //    return;
            //}
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
                    return;
                }
                Tech_Account account = Tech_Account.Load(JsonData.UserID);
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
                        return;
                    }
                }
                if (String.IsNullOrEmpty(JsonData.Password))
                {
                    BadLoginAttempts++;
                    account.BadLoginAttempts++;
                    account.LastBadLogin = DateTime.Now;
                    account.Save();
                    JsonData.Status = "invalid";
                    Send(Json.Encode(JsonData));
                    return;
                }
                if (JsonData.Password == account.TempPassword)
                {
                    if (String.IsNullOrEmpty(JsonData.NewPassword))
                    {
                        JsonData.Status = "new required";
                        Send(Json.Encode(JsonData));
                        return;
                    }
                    else if (JsonData.NewPassword != JsonData.ConfirmNewPassword)
                    {
                        JsonData.Status = "password mismatch";
                        Send(Json.Encode(JsonData));
                        return;
                    }
                    else if (JsonData.NewPassword.Length < 8 || JsonData.NewPassword.Length > 20)
                    {
                        JsonData.Status = "password length";
                        Send(Json.Encode(JsonData));
                        return;
                    }
                    else
                    {
                        ConnectionType = ConnectionTypes.Technician;
                        AuthenticationToken = Guid.NewGuid().ToString().Replace("-", "");
                        account.TempPassword = "";
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
                        TechAccount = account;
                        JsonData.Status = "ok";
                        JsonData.AuthenticationToken = AuthenticationToken;
                        JsonData.Access = TechAccount.AccessLevel.ToString();
                        Send(Json.Encode(JsonData));
                        return;
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
                    JsonData.Access = TechAccount.AccessLevel.ToString();
                    Send(Json.Encode(JsonData));
                    return;
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
                        JsonData.Access = TechAccount.AccessLevel.ToString();
                        JsonData.Status = "ok";
                        Send(Json.Encode(JsonData));
                        return;
                    }
                    else
                    {
                        BadLoginAttempts++;
                        JsonData.Status = "expired";
                        Send(Json.Encode(JsonData));
                        return;
                    }
                }
                // Bad login attempt.
                BadLoginAttempts++;
                account.BadLoginAttempts++;
                account.LastBadLogin = DateTime.Now;
                account.Save();
                JsonData.Status = "invalid";
                Send(Json.Encode(JsonData));
                return;
            }
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
        public void HandleGetCustomerFormInfo(dynamic JsonData)
        {
            // TODO: Use service data when available.
            //try
            //{
            //    var query = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
            //    // TODO: Test in AD environment.
            //    //ConnectionOptions conn = new ConnectionOptions();
            //    //conn.Username = "";
            //    //conn.Password = "";
            //    //conn.Authority = "ntlmdomain:DOMAIN";
            //    //var scope = new ManagementScope(String.Format("\\\\{0}\\root\\CIMV2", WebSocketContext.UserHostName), conn);
            //    //ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            //    ManagementObjectSearcher searcher = new ManagementObjectSearcher(String.Format("\\\\{0}\\root\\CIMV2", WebSocketContext.UserHostName ?? WebSocketContext.UserHostAddress), "SELECT * FROM Win32_ComputerSystem");
            //    ManagementObjectCollection collection = searcher.Get();
            //    foreach (ManagementObject mo in collection)
            //    {
            //        foreach (var moProp in mo.Properties)
            //        {
            //            JsonData[moProp.Name] = moProp?.Value?.ToString();
            //        }
            //    }
            //}
            //catch { }
            //Send(Json.Encode(JsonData));
        }
        public void HandleGetSupportCategories(dynamic JsonData)
        {
            var categories = new List<string>();
            foreach (var sc in Config.Current.Support_Categories)
            {
                categories.Add(sc.Category);
            }
            JsonData.Categories = categories.Distinct();
            Send(Json.Encode(JsonData));
        }
        public void HandleGetSupportTypes(dynamic JsonData)
        {
            var categories = Config.Current.Support_Categories.FindAll(sc => sc.Category == JsonData.SupportCategory);
            var types = new List<string>();
            foreach (var sc in categories)
            {
                types.Add(sc.Type);
            }
            JsonData.Types = types;
            Send(Json.Encode(JsonData));
        }
        public void HandleGetSupportQueue(dynamic JsonData)
        {
            var queue = Config.Current.Support_Categories.Find(sc => sc.Category == JsonData.SupportCategory && sc.Type == JsonData.SupportType);
            if (queue != null)
            {
                JsonData.SupportQueue = queue.Queue;
                Send(Json.Encode(JsonData));
            }
        }
        public void HandleEnterTechChat(dynamic JsonData)
        {
            if (!AuthenticateTech(JsonData))
            {
                return;
            }
            else
            {
                JsonData.Status = "ok";
                Send(Json.Encode(JsonData));
                LoggedIntoChat = true;
            }
        }
        public void HandleExitTechChat(dynamic JsonData)
        {
            if (Partner != null)
            {
                HandleSendToQueue(JsonData);
            }
            LoggedIntoChat = false;
        }
        public void HandleGetQueues(dynamic JsonData)
        {
            if (!AuthenticateTech(JsonData))
            {
                return;
            }
            var queues = new List<string>();
            foreach (var sc in Config.Current.Support_Categories)
            {
                queues.Add(sc.Queue);
            }
            queues.Sort();
            JsonData.Queues = queues.Distinct();
            Send(Json.Encode(JsonData));
        }
        public void HandleGetCases(dynamic JsonData)
        {
            if (!AuthenticateTech(JsonData))
            {
                return;
            }
            JsonData.Cases = Open_Cases;
            Send(Json.Encode(JsonData));
        }
        public void HandleTakeCase(dynamic JsonData)
        {
            if (!AuthenticateTech(JsonData))
            {
                return;
            }
            var takeCase = Open_Cases.Find(cs => cs.CaseID == JsonData.CaseID);
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
                WebMail.UserName = Config.Current.Email_SMTP_Username;
                WebMail.Password = Config.Current.Email_SMTP_Password;
                WebMail.From = Config.Current.Email_SMTP_Username;
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
                var transferCase = Open_Cases.Find(ca => ca.CaseID == (string)JsonData.CaseID);
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
            var lockCase = Open_Cases.Find(ca => ca.CaseID == int.Parse(JsonData.CaseID));
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

        #region Socket Message Handlers - Account Center
        public void HandleGetTechAccounts(dynamic JsonData)
        {
            if (!AuthenticateAdmin(JsonData))
            {
                return;
            }
            if (TechAccount.AccessLevel != Tech_Account.Access_Levels.Admin)
            {
                JsonData.Status = "unauthorized";
                Send(Json.Encode(JsonData));
                return;
            }
            JsonData.Status = "ok";
            JsonData.TechAccounts = Utilities.Tech_Accounts;
            Send(Json.Encode(JsonData));
        }
        public void HandleSaveTechAccount(dynamic JsonData)
        {
            if (!AuthenticateAdmin(JsonData))
            {
                return;
            }
            var account = Utilities.Tech_Accounts.Find(ta => ta.UserID == JsonData.Account.UserID);
            if (account == null)
            {
                JsonData.Status = "notfound";
                Send(Json.Encode(JsonData));
                return;
            }
            try
            {
                account = Json.Decode<Tech_Account>(Json.Encode(JsonData.Account));
                account.Save();
                JsonData.Status = "ok";
                Send(Json.Encode(JsonData));
            }
            catch
            {
                JsonData.Status = "failed";
                Send(Json.Encode(JsonData));
            }
            
        }
        public void HandleNewTechAccount(dynamic JsonData)
        {
            if (!AuthenticateAdmin(JsonData))
            {
                return;
            }
            if (Utilities.Tech_Accounts.Exists(ta => ta.UserID == JsonData.Account.UserID))
            {
                JsonData.Status = "exists";
                Send(Json.Encode(JsonData));
                return;
            }
            else if (JsonData.Account.UserID.Length < 3)
            {
                JsonData.Status = "length";
                Send(Json.Encode(JsonData));
                return;
            }
            else if (new Regex("[^0-9a-zA-Z]").IsMatch(JsonData.Account.UserID as string))
            {
                JsonData.Status = "invalid";
                Send(Json.Encode(JsonData));
                return;
            }
            try
            {
                var account = Json.Decode<Tech_Account>(Json.Encode(JsonData.Account));
                account.Save();
                JsonData.Status = "ok";
                JsonData.Account = account;
                Send(Json.Encode(JsonData));
            }
            catch
            {
                JsonData.Status = "failed";
                Send(Json.Encode(JsonData));
            }
        }
        public void HandleDeleteTechAccount(dynamic JsonData)
        {
            if (!AuthenticateAdmin(JsonData))
            {
                return;
            }
            var account = Utilities.Tech_Accounts.Find(ta => ta.UserID == JsonData.UserID);
            if (account == null)
            {
                JsonData.Status = "notfound";
                Send(Json.Encode(JsonData));
                return;
            }
            try
            {
                File.Delete(Path.Combine(Utilities.App_Data, "Tech_Accounts", JsonData.UserID + ".json"));
                JsonData.Status = "ok";
                Send(Json.Encode(JsonData));
            }
            catch
            {
                JsonData.Status = "failed";
                Send(Json.Encode(JsonData));
            }

        }
        public void HandleGetAllComputerGroups(dynamic JsonData)
        {
            if (!AuthenticateAdmin(JsonData))
            {
                return;
            }
            JsonData.Status = "ok";
            JsonData.ComputerGroups = Config.Current.Computer_Groups;
            Send(Json.Encode(JsonData));
        }
        #endregion

        #region Socket Message Handlers - Configuration
        public void HandleGetConfiguration(dynamic JsonData)
        {
            if (!AuthenticateAdmin(JsonData))
            {
                return;
            }
            JsonData.Status = "ok";
            JsonData.Config = Config.Current;
            Send(Json.Encode(JsonData));
        }
        public void HandleSetConfigProperty(dynamic JsonData)
        {
            if (!AuthenticateAdmin(JsonData))
            {
                return;
            }
            switch ((string)JsonData.Property)
            {
                case "Company_Name":
                    Config.Current.Company_Name = JsonData.Value;
                    break;
                case "Default_Admin":
                    Config.Current.Default_Admin = JsonData.Value;
                    break;
                case "Demo_Mode":
                    Config.Current.Demo_Mode = JsonData.Value;
                    break;
                case "File_Encryption":
                    Config.Current.File_Encryption = JsonData.Value;
                    break;
                case "Active_Directory_Enabled":
                    Config.Current.Active_Directory_Enabled = JsonData.Value;
                    break;
                case "AD_Tech_Group":
                    Config.Current.Active_Directory_Tech_Group = JsonData.Value;
                    break;
                case "AD_admin_Group":
                    Config.Current.Active_Directory_Admin_Group = JsonData.Value;
                    break;
                case "Feature_Chat":
                    Config.Current.Feature_Enabled_Chat = JsonData.Value;
                    break;
                case "Feature_Remote_Control":
                    Config.Current.Feature_Enabled_Remote_Control = JsonData.Value;
                    break;
                case "Feature_Account_Center":
                    Config.Current.Feature_Enabled_Account_Center = JsonData.Value;
                    break;
                case "Feature_Computer_Hub":
                    Config.Current.Feature_Enabled_Computer_Hub = JsonData.Value;
                    break;
                case "Feature_Configuration":
                    Config.Current.Feature_Enabled_Configuration = JsonData.Value;
                    break;
                case "Email_Server":
                    Config.Current.Email_SMTP_Server = JsonData.Value;
                    break;
                case "Email_Port":
                    Config.Current.Email_SMTP_Port = JsonData.Value;
                    break;
                case "Email_Username":
                    Config.Current.Email_SMTP_Username = JsonData.Value;
                    break;
                case "Email_Password":
                    Config.Current.Email_SMTP_Password = JsonData.Value;
                    break;
                case "Default_RC_Download":
                    Config.Current.Default_RC_Download = JsonData.Value;
                    break;
                default:
                    break;
            }
            Config.Save();
        }

        public void HandleSetSupportQueue(dynamic JsonData)
        {
            if (!AuthenticateAdmin(JsonData))
            {
                return;
            }
            try
            {
                var item = Config.Current.Support_Categories.Find(sc => sc.Category == JsonData.SupportCategory && sc.Type == JsonData.SupportType);
                item.Queue = JsonData.SupportQueue;
                Config.Save();
                JsonData.Status = "ok";
                Send(Json.Encode(JsonData));
            }
            catch
            {
                JsonData.Status = "failed";
                Send(Json.Encode(JsonData));
            }
        }
        public void HandleAddSupportCategory(dynamic JsonData)
        {
            if (!AuthenticateAdmin(JsonData))
            {
                return;
            }
            if (String.IsNullOrWhiteSpace(JsonData.SupportCategory))
            {
                JsonData.Status = "length";
                Send(Json.Encode(JsonData));
                return;
            }
            if (JsonData.SupportCategory.Length < 3)
            {
                JsonData.Status = "length";
                Send(Json.Encode(JsonData));
                return;
            }
            if (Config.Current.Support_Categories.Exists(sc=>sc.Category == JsonData.SupportCategory))
            {
                JsonData.Status = "exists";
                Send(Json.Encode(JsonData));
                return;
            }
            var comparer = StringComparer.Create(System.Globalization.CultureInfo.CurrentCulture, true);
            Config.Current.Support_Categories.Add(new Support_Category(JsonData.SupportCategory, "Other", "Other"));
            Config.Current.Support_Categories.Sort(new Comparison<Support_Category>((Support_Category a, Support_Category b) => {
                if (a.Category == b.Category)
                {
                    return comparer.Compare(a.Type, b.Type);
                }
                else
                {
                    return comparer.Compare(a.Category, b.Category);
                }
            }));
            Config.Save();
            JsonData.Status = "ok";
            Send(Json.Encode(JsonData));
        }
        public void HandleDeleteSupportCategory(dynamic JsonData)
        {
            if (!AuthenticateAdmin(JsonData))
            {
                return;
            }
            if (JsonData.SupportCategory == "Other")
            {
                return;
            }
            Config.Current.Support_Categories.RemoveAll(sc => sc.Category == JsonData.SupportCategory);
            Config.Save();
            JsonData.Status = "ok";
            Send(Json.Encode(JsonData));
        }
        public void HandleAddSupportType(dynamic JsonData)
        {
            if (!AuthenticateAdmin(JsonData))
            {
                return;
            }
            if (String.IsNullOrWhiteSpace(JsonData.SupportCategory) || String.IsNullOrWhiteSpace(JsonData.SupportType))
            {
                JsonData.Status = "length";
                Send(Json.Encode(JsonData));
                return;
            }
            if (JsonData.SupportType.Length < 3)
            {
                JsonData.Status = "length";
                Send(Json.Encode(JsonData));
                return;
            }
            if (Config.Current.Support_Categories.Exists(sc => sc.Category == JsonData.SupportCategory && sc.Type == JsonData.SupportType))
            {
                JsonData.Status = "exists";
                Send(Json.Encode(JsonData));
                return;
            }
            var comparer = StringComparer.Create(System.Globalization.CultureInfo.CurrentCulture, true);
            Config.Current.Support_Categories.Add(new Support_Category(JsonData.SupportCategory, JsonData.SupportType, "Other"));
            Config.Current.Support_Categories.Sort(new Comparison<Support_Category>((Support_Category a, Support_Category b) => {
                if (a.Category == b.Category)
                {
                    return comparer.Compare(a.Type, b.Type);
                }
                else
                {
                    return comparer.Compare(a.Category, b.Category);
                }
            }));
            Config.Save();
            JsonData.Status = "ok";
            Send(Json.Encode(JsonData));
        }
        public void HandleDeleteSupportType(dynamic JsonData)
        {
            if (!AuthenticateAdmin(JsonData))
            {
                return;
            }
            if (JsonData.SupportCategory == "Other")
            {
                return;
            }
            if (Config.Current.Support_Categories.Where(sc=>sc.Category == JsonData.SupportCategory).Count() == 1)
            {
                return;
            }
            Config.Current.Support_Categories.RemoveAll(sc => sc.Category == JsonData.SupportCategory && sc.Type == JsonData.SupportType);
            Config.Save();
            JsonData.Status = "ok";
            Send(Json.Encode(JsonData));
        }
        public void HandleAddSupportQueue(dynamic JsonData)
        {
            if (!AuthenticateAdmin(JsonData))
            {
                return;
            }
            try
            {
                var item = Config.Current.Support_Categories.Find(sc => sc.Category == JsonData.SupportCategory && sc.Type == JsonData.SupportType);
                item.Queue = JsonData.SupportQueue;
                Config.Save();
                JsonData.Status = "ok";
                Send(Json.Encode(JsonData));
            }
            catch
            {
                JsonData.Status = "failed";
                Send(Json.Encode(JsonData));
            }
        }
        public void HandleAddComputerGroup(dynamic JsonData)
        {
            if (String.IsNullOrEmpty(JsonData.Group))
            {
                return;
            }
            if (JsonData.Group.Length < 3)
            {
                return;
            }
            if (Config.Current.Computer_Groups.Contains(JsonData.Group))
            {
                JsonData.Status = "exists";
                Send(Json.Encode(JsonData));
                return;
            }
            Config.Current.Computer_Groups.Add(JsonData.Group);
            Config.Save();
            JsonData.Status = "ok";
            Send(Json.Encode(JsonData));
        }
        public void HandleDeleteComputerGroup(dynamic JsonData)
        {
            Config.Current.Computer_Groups.RemoveAll(cg => cg == JsonData.Group);
            Config.Save();
            JsonData.Status = "ok";
            Send(Json.Encode(JsonData));
        }
        #endregion
    }
}
