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
        public static List<Socket_Main> SocketCollection { get; } = new List<Socket_Main>();
        public static List<Socket_Main> Customers
        {
            get
            {
                return SocketCollection.FindAll(sc => sc.ConnectionType == ConnectionTypes.Customer);
            }
        }
        public static List<Socket_Main> AvailableTechs
        {
            get
            {
                return SocketCollection.FindAll(sc => sc.ConnectionType == ConnectionTypes.Technician && sc.LoggedIntoChat == true);
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
                foreach (Socket_Main sc in Socket_Main.SocketCollection.Where(sc => sc.ConnectionType == Socket_Main.ConnectionTypes.Customer && sc.Partner == null))
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
        public List<AuthenticationToken> AuthenticationTokens { get; set; } = new List<AuthenticationToken>();
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
                var error = new Exception("Type is null within Socket_Main.OnMessage.");
                Utilities.WriteToLog(error);
                throw error;
            }
            var methodHandler = Type.GetType("InstaTech.App_Code.Socket_Handlers.Socket_Main").GetMethods().FirstOrDefault(mi => mi.Name == "Handle" + jsonMessage.Type);
            if (methodHandler != null)
            {
                Utilities.LastMessageType = jsonMessage.Type;
                try
                {
                    methodHandler.Invoke(this, new object[] { jsonMessage });
                }
                catch (Exception ex)
                {
                    Utilities.WriteToLog(ex);
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
            var cases = SocketCollection.Where(sock => sock.ConnectionType == ConnectionTypes.Customer && sock.Partner == null && sock?.SupportCase?.DTCreated < SupportCase?.DTCreated);
            return cases.Count() + 1;
        }
        private bool AuthenticateTech(dynamic JsonData)
        {
            if (!AuthenticationTokens.Exists(at => at.Token == JsonData.AuthenticationToken))
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
            if (!AuthenticationTokens.Exists(at=>at.Token == JsonData.AuthenticationToken) || TechAccount.AccessLevel != Tech_Account.Access_Levels.Admin)
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
        public void LogFileDeployment(dynamic JsonData)
        {
            try
            {
                var filePath = Path.Combine(Utilities.App_Data, "Logs", "File_Deployments", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0') + ".txt");
                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                }
                var entry = new
                {
                    Timestamp = DateTime.Now.ToString(),
                    FileName = JsonData.FileName,
                    URL = JsonData.URL,
                    Arguments = JsonData.Arguments,
                    TargetComputer = JsonData.TargetComputer,
                    FromID = JsonData.FromID,
                    ExitCode = JsonData.ExitCode,
                    Output = JsonData.Output
                };
                File.AppendAllText(filePath, Json.Encode(entry) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void LogConsoleCommand(dynamic JsonData)
        {
            try
            {
                var filePath = Path.Combine(Utilities.App_Data, "Logs", "Console_Commands", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0') + ".txt");
                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                }
                var entry = new
                {
                    Timestamp = DateTime.Now.ToString(),
                    Language = JsonData.Language,
                    Command = Encoding.UTF8.GetString(Convert.FromBase64String(JsonData.Command)),
                    TargetComputer = JsonData.TargetComputer,
                    FromID = JsonData.FromID,
                    Output = JsonData.Output
                };
                File.AppendAllText(filePath, Json.Encode(entry) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        #endregion

        #region Socket Message Handlers - Shared
        public void HandleSessionEnded(dynamic JsonData)
        {
            try
            {
                if (ConnectionType == ConnectionTypes.Technician)
                {
                    Partner.SupportCase.DTClosed = DateTime.Now;
                    Partner.SupportCase.Status = Case.CaseStatus.Resolved;
                    Partner.SupportCase.Save();
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleTechMainLogin(dynamic JsonData)
        {
            try
            {
                if (BadLoginAttempts >= 3)
                {
                    JsonData.Status = "temp ban";
                    Send(Json.Encode(JsonData));
                    return;
                }
                if (Config.Current.Demo_Mode && JsonData.UserID.ToLower() == "demo" && JsonData.Password == "tech")
                {
                    var authToken = new AuthenticationToken() { Token = Guid.NewGuid().ToString().Replace("-", ""), LastUsed = DateTime.Now };
                    AuthenticationTokens.Add(authToken);
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
                        TechAccount.AuthenticationTokens.AddRange(AuthenticationTokens);
                    }
                    ConnectionType = ConnectionTypes.Technician;
                    TechAccount.Save();
                    JsonData.Status = "ok";
                    JsonData.Access = TechAccount.AccessLevel.ToString();
                    JsonData.AuthenticationToken = authToken.Token;
                    Send(Json.Encode(JsonData));
                    return;
                }
                //else if (Config.Current.Active_Directory_Enabled)
                //{
                //    // TODO: AD authentication.
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
                    Tech_Account account = Json.Decode<Tech_Account>(File.ReadAllText(Utilities.App_Data + "Tech_Accounts\\" + JsonData.UserID + ".json"));
                    account.AuthenticationTokens.RemoveAll(at => DateTime.Now - at.LastUsed > TimeSpan.FromDays(30));
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
                            var authToken = new AuthenticationToken() { Token = Guid.NewGuid().ToString().Replace("-", ""), LastUsed = DateTime.Now };
                            AuthenticationTokens.Add(authToken);
                            account.TempPassword = "";
                            account.HashedPassword = Crypto.HashPassword(JsonData.ConfirmNewPassword);
                            account.BadLoginAttempts = 0;
                            if (JsonData.RememberMe == true)
                            {
                                account.AuthenticationTokens.Add(authToken);
                            }
                            ConnectionType = ConnectionTypes.Technician;
                            account.Save();
                            if (SocketCollection.Exists(sock => sock?.TechAccount?.UserID == account.UserID))
                            {
                                foreach (var login in SocketCollection.FindAll(sock => sock?.TechAccount?.UserID == account.UserID))
                                {
                                    var request = new
                                    {
                                        Type = "NewLogin"
                                    };
                                    login.Send(Json.Encode(request));
                                    login.Close();
                                }
                            }
                            TechAccount = account;
                            JsonData.Status = "ok";
                            JsonData.Access = TechAccount.AccessLevel.ToString();
                            JsonData.AuthenticationToken = authToken.Token;
                            Send(Json.Encode(JsonData));
                            return;
                        }
                    }
                    if (Crypto.VerifyHashedPassword(account.HashedPassword, JsonData.Password))
                    {
                        var authToken = new AuthenticationToken() { Token = Guid.NewGuid().ToString().Replace("-", ""), LastUsed = DateTime.Now };
                        AuthenticationTokens.Add(authToken);
                        account.BadLoginAttempts = 0;
                        account.TempPassword = "";
                        if (JsonData.RememberMe == true)
                        {
                            account.AuthenticationTokens.Add(authToken);
                        }
                        ConnectionType = ConnectionTypes.Technician;
                        account.Save();
                        if (SocketCollection.Exists(sock => sock?.TechAccount?.UserID == account.UserID))
                        {
                            foreach (var login in SocketCollection.FindAll(sock => sock?.TechAccount?.UserID == account.UserID))
                            {
                                var request = new
                                {
                                    Type = "NewLogin"
                                };
                                login.Send(Json.Encode(request));
                                login.Close();
                            }
                        }
                        TechAccount = account;
                        JsonData.Status = "ok";
                        JsonData.Access = TechAccount.AccessLevel.ToString();
                        JsonData.AuthenticationToken = authToken.Token;
                        Send(Json.Encode(JsonData));
                        return;
                    }
                    if (!String.IsNullOrEmpty(JsonData.AuthenticationToken))
                    {
                        if (AuthenticationTokens.Exists(at => at.Token == JsonData.AuthenticationToken) || account.AuthenticationTokens.Exists(at=>at.Token == JsonData.AuthenticationToken))
                        {
                            var authToken = new AuthenticationToken() { Token = Guid.NewGuid().ToString().Replace("-", ""), LastUsed = DateTime.Now };
                            account.AuthenticationTokens.RemoveAll(at => at.Token == JsonData.AuthenticationToken);
                            AuthenticationTokens.Add(authToken);
                            if (JsonData.RememberMe == true)
                            {
                                account.AuthenticationTokens.Add(authToken);
                            }
                            ConnectionType = ConnectionTypes.Technician;
                            account.Save();
                            account.BadLoginAttempts = 0;
                            account.TempPassword = "";
                            account.Save();
                            if (SocketCollection.Exists(sock => sock?.TechAccount?.UserID == account.UserID))
                            {
                                foreach (var login in SocketCollection.FindAll(sock => sock?.TechAccount?.UserID == account.UserID))
                                {
                                    var request = new
                                    {
                                        Type = "NewLogin"
                                    };
                                    login.Send(Json.Encode(request));
                                    login.Close();
                                }
                            }
                            TechAccount = account;
                            JsonData.Status = "ok";
                            JsonData.Access = TechAccount.AccessLevel.ToString();
                            JsonData.AuthenticationToken = authToken.Token;
                            Send(Json.Encode(JsonData));
                        }
                        else
                        {
                            BadLoginAttempts++;
                            JsonData.Status = "expired";
                            Send(Json.Encode(JsonData));
                        }
                        return;
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        #endregion

        #region Socket Message Handlers - Chat
        public void HandleCustomerChatLogin(dynamic JsonData)
        {
            try
            {
                // Prevent tech from logging in as a customer.
                if (ConnectionType != null)
                {
                    JsonData.Status = "loggedin";
                    Send(Json.Encode(JsonData));
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
                if (SupportCase.SupportCategory == "Other")
                {
                    SupportCase.SupportType = "Other";
                }
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
                AvailableTechs.ForEach((Socket_Main sc) =>
                {
                    sc.Send(request);
                });
                if (Config.Current.Demo_Mode && AvailableTechs.Count == 0)
                {
                    TechBot.Notify(this);
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleGetCustomerFormInfo(dynamic JsonData)
        {
            try
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleGetSupportCategories(dynamic JsonData)
        {
            try
            {
                var categories = new List<string>();
                foreach (var sc in Config.Current.Support_Categories)
                {
                    categories.Add(sc.Category);
                }
                JsonData.Categories = categories.Distinct();
                Send(Json.Encode(JsonData));
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleGetSupportTypes(dynamic JsonData)
        {
            try
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleGetSupportQueue(dynamic JsonData)
        {
            try
            {
                var queue = Config.Current.Support_Categories.Find(sc => sc.Category == JsonData.SupportCategory && sc.Type == JsonData.SupportType);
                if (queue != null)
                {
                    JsonData.SupportQueue = queue.Queue;
                    Send(Json.Encode(JsonData));
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleEnterTechChat(dynamic JsonData)
        {
            try
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleExitTechChat(dynamic JsonData)
        {
            try
            {
                if (Partner != null)
                {
                    HandleSendToQueue(JsonData);
                }
                LoggedIntoChat = false;
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleGetQueues(dynamic JsonData)
        {
            try
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleGetCases(dynamic JsonData)
        {
            try
            {
                if (!AuthenticateTech(JsonData))
                {
                    return;
                }
                JsonData.Cases = Open_Cases;
                Send(Json.Encode(JsonData));
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleTakeCase(dynamic JsonData)
        {
            try
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
                Partner = SocketCollection.Find(sc => sc?.SupportCase?.CaseID == takeCase.CaseID);
                //SupportCase = Partner.SupportCase;
                Partner.Partner = this;
                takeCase.Save();
                JsonData.Status = "ok";
                JsonData.TechID = TechAccount.UserID;
                JsonData.TechFirstName = TechAccount.FirstName;
                JsonData.TechLastName = TechAccount.LastName;
                Partner.Send(Json.Encode(JsonData));
                JsonData.PreviousMessages = Partner.SupportCase.Messages;
                Send(Json.Encode(JsonData));
                foreach (Socket_Main socket in Customers.Where(cu => cu.Partner == null))
                {
                    socket.SendWaitUpdate();
                }
                var request = Json.Encode(new
                {
                    Type = "CaseUpdate",
                    Status = "Remove",
                    Case = Partner.SupportCase
                });
                AvailableTechs.ForEach((Socket_Main sc) =>
                {
                    sc.Send(request);
                });
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleChatMessage(dynamic JsonData)
        {
            try
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
                    message.Content = Encoding.UTF8.GetString(Convert.FromBase64String(JsonData.Message));
                    Partner.SupportCase.Messages.Add(message);
                    Partner.SupportCase.Save();
                }
                else
                {
                    message.FromUserID = SupportCase.CustomerUserID;
                    message.Content = Encoding.UTF8.GetString(Convert.FromBase64String(JsonData.Message));
                    SupportCase.Messages.Add(message);
                    SupportCase.Save();
                }
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleTyping(dynamic JsonData)
        {
            try
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleForgotPassword(dynamic JsonData)
        {
            try
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleSendToQueue(dynamic JsonData)
        {
            try
            {
                Partner.Send(Json.Encode(new { Type = "SendToQueue" }));
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleCaseUpdate(dynamic JsonData)
        {
            try
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleLockCase(dynamic JsonData)
        {
            try
            {
                var lockCase = Open_Cases.Find(ca => ca.CaseID == JsonData.CaseID);
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
                new Timer((object lockedCase) =>
                {
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        #endregion

        #region Socket Message Handlers - Account Center
        public void HandleGetTechAccounts(dynamic JsonData)
        {
            try
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
                foreach (Tech_Account account in JsonData.TechAccounts)
                {
                    account.HashedPassword = null;
                    account.AuthenticationTokens.Clear();
                }
                Send(Json.Encode(JsonData));
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleSaveTechAccount(dynamic JsonData)
        {
            try
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
                    var newAccount = Json.Decode<Tech_Account>(Json.Encode(JsonData.Account));
                    foreach (System.Reflection.PropertyInfo prop in typeof(Tech_Account).GetProperties())
                    {
                        if (prop.Name == "AuthenticationTokens" || prop.Name == "HashedPassword")
                        {
                            continue;
                        }
                        prop.SetValue(account, prop.GetValue(newAccount));
                    }
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }

        }
        public void HandleNewTechAccount(dynamic JsonData)
        {
            try
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleDeleteTechAccount(dynamic JsonData)
        {
            try
            {
                if (!AuthenticateAdmin(JsonData))
                {
                    return;
                }
                var allAccounts = Utilities.Tech_Accounts;
                var account = allAccounts.Find(ta => ta.UserID == JsonData.UserID);
                if (account == null)
                {
                    JsonData.Status = "notfound";
                    Send(Json.Encode(JsonData));
                    return;
                }
                if (account.AccessLevel == Tech_Account.Access_Levels.Admin && allAccounts.Count(acct=>acct.AccessLevel == Tech_Account.Access_Levels.Admin) == 1)
                {
                    JsonData.Status = "last";
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }

        }
        public void HandleGetAllComputerGroups(dynamic JsonData)
        {
            try
            {
                if (!AuthenticateTech(JsonData))
                {
                    return;
                }
                JsonData.Status = "ok";
                JsonData.ComputerGroups = Config.Current.Computer_Groups;
                Send(Json.Encode(JsonData));
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        #endregion

        #region Socket Message Handlers - Configuration
        public void HandleGetConfiguration(dynamic JsonData)
        {
            try
            {
                if (!AuthenticateAdmin(JsonData))
                {
                    return;
                }
                JsonData.Status = "ok";
                JsonData.Config = Config.Current;
                Send(Json.Encode(JsonData));
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleSetConfigProperty(dynamic JsonData)
        {
            try
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
                        if (Config.Current.File_Encryption)
                        {
                            Utilities.Set_File_Encryption(true);
                        }
                        else
                        {
                            Utilities.Set_File_Encryption(false);
                        }
                        break;
                    case "Active_Directory_Enabled":
                        Config.Current.Active_Directory_Enabled = JsonData.Value;
                        break;
                    case "Session_Recording":
                        Config.Current.Session_Recording = JsonData.Value;
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
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
            try
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
                if (Config.Current.Support_Categories.Exists(sc => sc.Category == JsonData.SupportCategory))
                {
                    JsonData.Status = "exists";
                    Send(Json.Encode(JsonData));
                    return;
                }
                var comparer = StringComparer.Create(System.Globalization.CultureInfo.CurrentCulture, true);
                Config.Current.Support_Categories.Add(new Support_Category(JsonData.SupportCategory, "Other", "Other"));
                Config.Current.Support_Categories.Sort(new Comparison<Support_Category>((Support_Category a, Support_Category b) =>
                {
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleDeleteSupportCategory(dynamic JsonData)
        {
            try
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleAddSupportType(dynamic JsonData)
        {
            try
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
                Config.Current.Support_Categories.Sort(new Comparison<Support_Category>((Support_Category a, Support_Category b) =>
                {
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleDeleteSupportType(dynamic JsonData)
        {
            try
            {
                if (!AuthenticateAdmin(JsonData))
                {
                    return;
                }
                if (JsonData.SupportCategory == "Other")
                {
                    return;
                }
                if (Config.Current.Support_Categories.Where(sc => sc.Category == JsonData.SupportCategory).Count() == 1)
                {
                    return;
                }
                Config.Current.Support_Categories.RemoveAll(sc => sc.Category == JsonData.SupportCategory && sc.Type == JsonData.SupportType);
                Config.Save();
                JsonData.Status = "ok";
                Send(Json.Encode(JsonData));
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
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
            try
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleDeleteComputerGroup(dynamic JsonData)
        {
            try
            {
                Config.Current.Computer_Groups.RemoveAll(cg => cg == JsonData.Group);
                Config.Save();
                JsonData.Status = "ok";
                Send(Json.Encode(JsonData));
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        #endregion

        #region Socket Message Handlers - Computer Hub
        public void HandleSearchComputerHub(dynamic JsonData)
        {
            try
            {
                if (!AuthenticateTech(JsonData))
                {
                    return;
                }
                var sockets = Remote_Control.SocketCollection;
                List<Remote_Control> services = new List<Remote_Control>();
                if (TechAccount.AccessLevel == Tech_Account.Access_Levels.Admin)
                {
                    var search = sockets.FindAll(rc => rc.ConnectionType == Remote_Control.ConnectionTypes.ClientService && rc.ComputerGroup.Contains(JsonData.SearchGroup));
                    if (search != null)
                    {
                        services.AddRange(search);
                    }
                }
                else if (!String.IsNullOrWhiteSpace(JsonData.SearchGroup) && !TechAccount.ComputerGroups.Contains((string)JsonData.SearchGroup))
                {
                    JsonData.Status = "denied";
                    Send(Json.Encode(JsonData));
                    return;
                }
                else
                {
                    var search = sockets.FindAll(rc => rc.ConnectionType == Remote_Control.ConnectionTypes.ClientService && TechAccount.ComputerGroups.Contains(rc.ComputerGroup));
                    if (!String.IsNullOrWhiteSpace(JsonData.SearchGroup))
                    {
                        search.RemoveAll(rc => !rc.ComputerGroup.Contains(JsonData.SearchGroup));
                    }
                    if (search != null)
                    {
                        services.AddRange(search);
                    }
                }
                if (JsonData.SearchBy == "Computer")
                {
                    services.RemoveAll(rc => !rc.ComputerName.Contains(JsonData.SearchString));
                }
                else if (JsonData.SearchBy == "User")
                {
                    services.RemoveAll(rc => !rc.CurrentUser.Contains(JsonData.SearchString) && !rc.LastLoggedOnUser.Contains(JsonData.SearchString));
                }
                else
                {
                    services.Clear();
                }
                var returnList = new List<dynamic>();
                foreach (var socket in services)
                {
                    returnList.Add(new
                    {
                        ComputerName = socket.ComputerName,
                        LastReboot = socket.LastReboot,
                        CurrentUser = socket.CurrentUser,
                        LastLoggedOnUser = socket.LastLoggedOnUser,
                        ComputerGroup = socket.ComputerGroup
                    });
                }
                JsonData.Status = "ok";
                JsonData.Computers = returnList;
                Send(Json.Encode(JsonData));
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleSetComputerGroup(dynamic JsonData)
        {
            try
            {
                if (!AuthenticateTech(JsonData))
                {
                    return;
                }
                if (String.IsNullOrWhiteSpace(JsonData.ComputerName))
                {
                    return;
                }
                if (!TechAccount.ComputerGroups.Contains(JsonData.ComputerGroup) && TechAccount.AccessLevel != Tech_Account.Access_Levels.Admin)
                {
                    JsonData.Status = "denied";
                    Send(Json.Encode(JsonData));
                    return;
                }
                var socket = Remote_Control.SocketCollection.Find(rc => (rc as Remote_Control).ComputerName == JsonData.ComputerName);
                if (socket != null)
                {
                    (socket as Remote_Control).ComputerGroup = JsonData.ComputerGroup;
                    (socket as Remote_Control).Save();
                    JsonData.Status = "ok";
                    Send(Json.Encode(JsonData));
                    return;
                }
                else
                {
                    JsonData.Status = "unknown";
                    Send(Json.Encode(JsonData));
                    return;
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleFileDeploy(dynamic JsonData)
        {
            try
            {
                if (!AuthenticateTech(JsonData))
                {
                    return;
                }
                var target = Remote_Control.SocketCollection.Find(rc => rc.ConnectionType == Remote_Control.ConnectionTypes.ClientService && rc.ComputerName == JsonData.TargetComputer);
                if (target == null)
                {
                    JsonData.Status = "notfound";
                    Send(Json.Encode(JsonData));
                    return;
                }
                if (!TechAccount.ComputerGroups.Contains(target.ComputerGroup) && TechAccount.AccessLevel != Tech_Account.Access_Levels.Admin)
                {
                    JsonData.Status = "denied";
                    Send(Json.Encode(JsonData));
                    return;
                }
                target.Send(Json.Encode(JsonData));
                // Response is received on Remote_Control socket.
                LogFileDeployment(JsonData);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleConsoleCommand(dynamic JsonData)
        {
            try
            {
                if (!AuthenticateTech(JsonData))
                {
                    return;
                }
                var target = Remote_Control.SocketCollection.Find(rc => rc.ConnectionType == Remote_Control.ConnectionTypes.ClientService && rc.ComputerName == JsonData.TargetComputer);
                if (target == null)
                {
                    JsonData.Status = "notfound";
                    Send(Json.Encode(JsonData));
                    return;
                }
                if (!TechAccount.ComputerGroups.Contains(target.ComputerGroup) && TechAccount.AccessLevel != Tech_Account.Access_Levels.Admin)
                {
                    JsonData.Status = "denied";
                    Send(Json.Encode(JsonData));
                    return;
                }
                target.Send(Json.Encode(JsonData));
                // Response is received on Remote_Control socket.
                LogConsoleCommand(JsonData);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleNewConsole(dynamic JsonData)
        {
            try
            {
                if (!AuthenticateTech(JsonData))
                {
                    return;
                }
                var target = Remote_Control.SocketCollection.Find(rc => rc.ConnectionType == Remote_Control.ConnectionTypes.ClientService && rc.ComputerName == JsonData.TargetComputer);
                if (target == null)
                {
                    JsonData.Status = "notfound";
                    Send(Json.Encode(JsonData));
                    return;
                }
                if (!TechAccount.ComputerGroups.Contains(target.ComputerGroup) && TechAccount.AccessLevel != Tech_Account.Access_Levels.Admin)
                {
                    JsonData.Status = "denied";
                    Send(Json.Encode(JsonData));
                    return;
                }
                target.Send(Json.Encode(JsonData));
                // Response is received on Remote_Control socket.
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        #endregion
    }
}
