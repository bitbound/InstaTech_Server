using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Web.WebSockets;
using System.Web.Helpers;
using InstaTech.App_Code.Models;
using System.IO;
using System.Text;
using System.Timers;

namespace InstaTech.App_Code.Socket_Handlers
{
    public class Remote_Control : WebSocketHandler
    {

        #region User-Defined Properties.
        public static List<Remote_Control> SocketCollection { get; } = new List<Remote_Control>();
        public string SessionID { get; set; }
        public Remote_Control Partner { get; set; }
        public string ComputerName { get; set; } = "";
        public string ComputerGroup { get; set; } = "";
        public DateTime LastReboot { get; set; }
        public string CurrentUser { get; set; } = "";
        public string LastLoggedOnUser { get; set; } = "";
        public List<string> AuthenticationTokens { get; set; } = new List<string>();
        public int BadLoginAttempts { get; set; } = 0;
        public Tech_Account TechAccount { get; set; }
        public ConnectionTypes? ConnectionType { get; set; }
        private Timer PingTimer { get; } = new Timer();
        private DateTime LastHeartbeatReceived { get; set; } = DateTime.Now;

        public enum ConnectionTypes
        {
            ClientApp,
            ViewerApp,
            ClientService,
            ClientServiceOnce,
            ClientConsole,
            ClientConsoleOnce
        }
        #endregion


        public Remote_Control()
        {
            this.MaxIncomingMessageSize = 999999999;
        }
        public override void OnOpen()
        {
            var random = new Random();
            SessionID = random.Next(0, 999).ToString().PadLeft(3, '0') + random.Next(0, 999).ToString().PadLeft(3, '0');
            SocketCollection.Add(this);
        }
        public override void OnClose()
        {
            SocketCollection.Remove(this);
            if (Partner != null)
            {
                var request = new
                {
                    Type = "PartnerClose"
                };
                Partner.Send(Json.Encode(request));
                Partner.Close();
                Partner.Partner = null;
                Partner = null;

            }
            if (ConnectionType == ConnectionTypes.ClientService)
            {
                Save();
            }
        }
        public override void OnError()
        {
            SocketCollection.Remove(this);
            if (Partner != null)
            {
                var request = new
                {
                    Type = "PartnerError"
                };
                Partner.Send(Json.Encode(request));
                Partner.Close();
                Partner.Partner = null;
                Partner = null;
            }
            if (ConnectionType == ConnectionTypes.ClientService)
            {
                Save();
            }
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
        }
        public override void OnMessage(byte[] message)
        {
            if (Config.Current.Session_Recording && Partner != null)
            {
                var videoFolder = Directory.CreateDirectory($@"{Utilities.App_Data}\Logs\Recordings\{Partner?.TechAccount?.UserID ?? Partner?.SessionID}\{DateTime.Now.Year.ToString()}\{DateTime.Now.Month.ToString().PadLeft(2, '0')}\{DateTime.Now.Day.ToString().PadLeft(2, '0')}\");
                var videoFile = Path.Combine(videoFolder.FullName, Partner.SessionID + ".itr");
                var base64 = Convert.ToBase64String(message);
                File.AppendAllText(videoFile, $"{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt")},{base64}{Environment.NewLine}");
            }
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
                var error = new Exception("Type is null within Remote_Control.OnMessage.");
                Utilities.WriteToLog(error);
                throw error;
            }
            var methodHandler = Type.GetType("InstaTech.App_Code.Socket_Handlers.Remote_Control").GetMethods().FirstOrDefault(mi => mi.Name == "Handle" + jsonMessage.Type);
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
            else
            {
                if (Partner != null)
                {
                    Partner.Send(message);
                }
            }
        }
        public void HandleTechMainLogin(dynamic JsonData) {
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
                    var authToken = Guid.NewGuid().ToString().Replace("-", "");
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
                    TechAccount.Save();
                    JsonData.Status = "ok";
                    JsonData.AuthenticationToken = authToken;
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
                    while (account.AuthenticationTokens.Count > 10)
                    {
                        account.AuthenticationTokens.RemoveAt(0);
                    }
                    
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
                            var authToken = Guid.NewGuid().ToString().Replace("-", "");
                            AuthenticationTokens.Add(authToken);
                            account.TempPassword = "";
                            account.HashedPassword = Crypto.HashPassword(JsonData.ConfirmNewPassword);
                            account.BadLoginAttempts = 0;
                            if (JsonData.RememberMe == true)
                            {
                                account.AuthenticationTokens.Add(authToken);
                            }
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
                            JsonData.AuthenticationToken = authToken;
                            Send(Json.Encode(JsonData));
                            return;
                        }
                    }
                    if (Crypto.VerifyHashedPassword(account.HashedPassword, JsonData.Password))
                    {
                        var authToken = Guid.NewGuid().ToString().Replace("-", "");
                        AuthenticationTokens.Add(authToken);
                        account.BadLoginAttempts = 0;
                        account.TempPassword = "";
                        if (JsonData.RememberMe == true)
                        {
                            account.AuthenticationTokens.Add(authToken);
                        }
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
                        JsonData.AuthenticationToken = authToken;
                        Send(Json.Encode(JsonData));
                        return;
                    }
                    if (!String.IsNullOrEmpty(JsonData.AuthenticationToken))
                    {
                        if (AuthenticationTokens.Contains(JsonData.AuthenticationToken) || account.AuthenticationTokens.Contains(JsonData.AuthenticationToken))
                        {
                            var authToken = Guid.NewGuid().ToString().Replace("-", "");
                            account.AuthenticationTokens.Remove(JsonData.AuthenticationToken);
                            AuthenticationTokens.Add(authToken);
                            if (JsonData.RememberMe == true)
                            {
                                account.AuthenticationTokens.Add(authToken);
                            }
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
                            JsonData.AuthenticationToken = authToken;
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
        public void HandleForgotPassword(dynamic JsonData) {
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
                JsonData.Access = TechAccount.AccessLevel.ToString();
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
        public void HandleConnectionType (dynamic JsonData)
        {
            try
            {
                ConnectionType = Enum.Parse(typeof(ConnectionTypes), JsonData.ConnectionType.ToString());
                if (ConnectionType == ConnectionTypes.ClientApp || ConnectionType == ConnectionTypes.ViewerApp)
                {
                    if (!String.IsNullOrWhiteSpace(JsonData.ComputerName))
                    {
                        ComputerName = JsonData.ComputerName.ToString().Trim().ToLower();
                    }
                    var sessionID = SessionID.Substring(0, 3) + " " + SessionID.Substring(3, 3);
                    var request = new
                    {
                        Type = "SessionID",
                        SessionID = sessionID
                    };
                    Send(Json.Encode(request));
                }
                else if (ConnectionType == ConnectionTypes.ClientConsole)
                {
                    ComputerName = JsonData.ComputerName.ToString().Trim().ToLower();
                }
                else if (ConnectionType == ConnectionTypes.ClientService)
                {
                    var client = SocketCollection.Find(sock => (sock as Remote_Control).ComputerName == JsonData.ComputerName);
                    if (client != null)
                    {
                        JsonData.Status = "ServiceDuplicate";
                        Send(Json.Encode(JsonData));
                        SocketCollection.Remove(this);
                        var filePath = Path.Combine(Utilities.App_Data, "Logs", "Alerts", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0') + ".txt");
                        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        }
                        var entry = new
                        {
                            Timestamp = DateTime.Now.ToString(),
                            Message = "The service may be running on two computers that have the same name.",
                            ComputerName = JsonData.ComputerName.ToString().Trim(),
                            IPAddress = WebSocketContext.UserHostAddress
                        };
                        File.AppendAllText(filePath, Json.Encode(entry) + Environment.NewLine);
                    }
                    ComputerName = JsonData.ComputerName.ToString().Trim().ToLower();
                    CurrentUser = JsonData?.CurrentUser?.ToString()?.Trim()?.ToLower() ?? "";
                    if (JsonData.LastReboot != null)
                    {
                        LastReboot = JsonData.LastReboot;
                    }
                    Load();
                    Save();
                    PingTimer.Interval = 30000;
                    PingTimer.Elapsed += (sender, args) =>
                    {
                        if (!SocketCollection.Contains(this))
                        {
                            Close();
                            return;
                        }
                        if (DateTime.Now - LastHeartbeatReceived > TimeSpan.FromMinutes(1))
                        {
                            Close();
                            return;
                        }
                        var ping = new
                        {
                            Type = "Ping"
                        };
                        Send(Json.Encode(ping));
                    };
                }
                else if (ConnectionType == ConnectionTypes.ClientServiceOnce)
                {
                    ComputerName = JsonData.ComputerName.ToString().Trim().ToLower();
                }
                else if (ConnectionType == ConnectionTypes.ClientConsoleOnce)
                {
                    ComputerName = JsonData.ComputerName.ToString().Trim().ToLower();
                }
                LogConnection();
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleHeartbeat(dynamic JsonData)
        {
            try
            {
                if (!SocketCollection.Contains(this))
                {
                    Close();
                    return;
                }
                LastHeartbeatReceived = DateTime.Now;
                ComputerName = JsonData?.ComputerName?.ToString()?.Trim()?.ToLower() ?? ComputerName ?? "";
                CurrentUser = JsonData?.CurrentUser?.ToString()?.Trim()?.ToLower() ?? "";
                if (JsonData.LastReboot != null) {
                    if (JsonData.LastReboot != null)
                    {
                        LastReboot = JsonData.LastReboot;
                    }
                }
                Save();
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleSearchComputers(dynamic JsonData) {
            try
            {
                if (!AuthenticateTech(JsonData))
                {
                    return;
                }
                var computers = new List<string>();
                var clients = SocketCollection.FindAll(rc => rc.ConnectionType == ConnectionTypes.ClientService);
                if (TechAccount.AccessLevel == Tech_Account.Access_Levels.Admin)
                {
                    foreach (var client in clients.Where(client=>client.ComputerName.ToLower().Contains(JsonData.Input.ToString().ToLower())))
                    {
                        computers.Add(client.ComputerName);
                    }
                }
                else
                {
                    foreach (var client in clients.Where(rc=>TechAccount.ComputerGroups.Contains(rc.ComputerGroup) && rc.ComputerName.ToLower().Contains(JsonData.Input.ToString().ToLower())))
                    {
                        computers.Add(client.ComputerName);
                    }
                }
                JsonData.Computers = computers.Distinct();
                Send(Json.Encode(JsonData));
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleConnect(dynamic JsonData)
        {
            try
            {
                var client = SocketCollection.Find(sock => sock.SessionID == JsonData.SessionID.ToString().Replace(" ", "") && sock.ConnectionType == ConnectionTypes.ClientApp);
                if (client != null)
                {
                    if ((client as Remote_Control).Partner != null)
                    {
                        JsonData.Status = "AlreadyHasPartner";
                        Send(Json.Encode(JsonData));
                    }
                    else
                    {
                        this.Partner = client;
                        client.Partner = this;
                        JsonData.Status = "ok";
                        Send(Json.Encode(JsonData));
                        client.Send(Json.Encode(JsonData));
                        LogSession();
                    }
                }
                else
                {
                    JsonData.Status = "InvalidID";
                    Send(Json.Encode(JsonData));
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleConnectUnattended(dynamic JsonData)
        {
            try
            {
                var serviceClient = SocketCollection.Find(sock => sock.ComputerName == JsonData.ComputerName.ToString().Trim().ToLower() && sock.ConnectionType == ConnectionTypes.ClientService);
                if (!TechAccount.ComputerGroups.Contains(serviceClient?.ComputerGroup) && TechAccount.AccessLevel != Tech_Account.Access_Levels.Admin)
                {
                    JsonData.Status = "unauthorized";
                    Send(Json.Encode(JsonData));
                    return;
                }
                var consoleClient = SocketCollection.Find(sock => sock.ComputerName == JsonData.ComputerName.ToString().Trim().ToLower() && sock.ConnectionType == ConnectionTypes.ClientConsole);
                if (consoleClient != null)
                {
                    if (consoleClient.Partner != null)
                    {
                        if (consoleClient.Partner?.TechAccount?.UserID == TechAccount.UserID)
                        {
                            consoleClient.Partner.Close();
                        }
                        else
                        {
                            JsonData.Status = "AlreadyHasPartner";
                            Send(Json.Encode(JsonData));
                            return;
                        }
                    }
                }
                if (serviceClient != null)
                {
                    if (serviceClient.Partner != null)
                    {
                        if (serviceClient.Partner?.TechAccount?.UserID == TechAccount.UserID)
                        {
                            serviceClient.Partner.Close();
                        }
                        else
                        {
                            JsonData.Status = "AlreadyHasPartner";
                            Send(Json.Encode(JsonData));
                            return;
                        }
                    }
                    this.Partner = serviceClient;
                    serviceClient.Partner = this;
                    serviceClient.Send(Json.Encode(JsonData));
                    LogSession();
                    return;
                }
                else
                {
                    JsonData.Status = "UnknownComputer";
                    Send(Json.Encode(JsonData));
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleQuickConnect(dynamic JsonData)
        {
            var techSocket = Socket_Main.SocketCollection.Find(sm => sm?.TechAccount?.UserID.ToLower() == JsonData?.UserID.ToLower());
            if (techSocket == null || !techSocket?.AuthenticationTokens?.Contains(JsonData?.AuthenticationToken))
            {
                JsonData.Status = "denied";
                Send(Json.Encode(JsonData));
                return;
            }
            ConnectionType = ConnectionTypes.ViewerApp;
            TechAccount = (Tech_Account)techSocket.TechAccount.Clone();
            AuthenticationTokens.AddRange(techSocket.AuthenticationTokens);
            var customerSocket = SocketCollection.Find(rc => rc.ConnectionType == ConnectionTypes.ClientService && rc.ComputerName == JsonData.ComputerName);
            if (customerSocket == null)
            {
                JsonData.Status = "notfound";
                Send(Json.Encode(JsonData));
                return;
            }
            if (TechAccount.AccessLevel != Tech_Account.Access_Levels.Admin && !TechAccount.ComputerGroups.Contains(customerSocket.ComputerGroup))
            {
                JsonData.Status = "unauthorized";
                Send(Json.Encode(JsonData));
                return;
            }
            if (customerSocket.Partner != null)
            {
                customerSocket.Partner.Close();
            }
            customerSocket.Partner = this;
            this.Partner = customerSocket;
            var request = new
            {
                Type = "ConnectUnattended",
                ComputerName = customerSocket.ComputerName,
                AuthenticationToken = JsonData.AuthenticationToken
            };
            customerSocket.Send(Json.Encode(request));
            LogSession();
        }
        public void HandleConnectUpgrade(dynamic JsonData)
        {
            try
            {
                if (Partner == null)
                {
                    JsonData.Status = "nopartner";
                    Send(Json.Encode(JsonData));
                    return;
                }
                var startTime = DateTime.Now;
                Remote_Control serviceClient = null;
                while (serviceClient == null)
                {
                    if (DateTime.Now - startTime > TimeSpan.FromSeconds(10))
                    {
                        break;
                    }
                    System.Threading.Thread.Sleep(200);
                    serviceClient = SocketCollection.Find(sock => sock.ComputerName == JsonData.ComputerName.ToString().Trim().ToLower() && sock.ConnectionType == ConnectionTypes.ClientServiceOnce);
                }
                if (serviceClient != null)
                {
                    var request = new
                    {
                        Type = "ConnectUnattendedOnce"
                    };
                    serviceClient.Send(Json.Encode(request));
                    startTime = DateTime.Now;
                    Remote_Control consoleClient = null;
                    while (consoleClient == null)
                    {
                        if (DateTime.Now - startTime > TimeSpan.FromSeconds(10))
                        {
                            break;
                        }
                        System.Threading.Thread.Sleep(200);
                        consoleClient = SocketCollection.Find(sock => sock.ComputerName == JsonData.ComputerName.ToString().Trim().ToLower() && sock.ConnectionType == ConnectionTypes.ClientConsoleOnce);
                    }
                    if (consoleClient != null)
                    {
                        var viewer = Partner;
                        consoleClient.Partner = viewer;
                        viewer.Partner = consoleClient;
                        Partner = null;
                        JsonData.Status = "ok";
                        Send(Json.Encode(JsonData));
                        viewer.Send(Json.Encode(JsonData));
                        return;
                    }
                    else
                    {
                        JsonData.Status = "timeout";
                        Send(Json.Encode(JsonData));
                    }
                }
                else
                {
                    JsonData.Status = "timeout";
                    Send(Json.Encode(JsonData));
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleProcessStartResult(dynamic JsonData)
        {
            try
            {
                var partner = Partner;
                Partner.Partner = null;
                Partner = null;
                if (JsonData.Status == "ok")
                {
                    var started = DateTime.Now;
                    var success = true;
                    while (SocketCollection.Where(sock => sock.ComputerName == ComputerName && sock.ConnectionType == ConnectionTypes.ClientConsole).Count() == 0)
                    {
                        System.Threading.Thread.Sleep(200);
                        if (DateTime.Now - started > TimeSpan.FromSeconds(5))
                        {
                            success = false;
                            break;
                        }
                    }
                    if (success)
                    {
                        partner.Send(Json.Encode(JsonData));
                    }
                    else
                    {
                        JsonData.Status = "failed";
                        partner.Send(Json.Encode(JsonData));
                    }
                }
                else
                {
                    JsonData.Status = "failed";
                    partner.Send(Json.Encode(JsonData));
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleCompleteConnection(dynamic JsonData)
        {
            try
            {
                if (!AuthenticateTech(JsonData))
                {
                    return;
                }
                var consoleClient = SocketCollection.Find(sock => sock.ComputerName == JsonData.ComputerName.ToString().Trim().ToLower() && sock.ConnectionType == ConnectionTypes.ClientConsole);
                if (consoleClient != null)
                {
                    if (consoleClient.Partner != null)
                    {
                        JsonData.Status = "AlreadyHasPartner";
                        Send(Json.Encode(JsonData));
                        return;
                    }
                    else
                    {
                        this.Partner = consoleClient;
                        consoleClient.Partner = this;
                        JsonData.Status = "ok";
                        Send(Json.Encode(JsonData));
                        LogSession();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleDesktopSwitch(dynamic JsonData)
        {
            try
            {
                Partner.Send(Json.Encode(JsonData));
                var startTime = DateTime.Now;
                JsonData.Status = "ok";
                while (!SocketCollection.Exists(sock => sock.ComputerName == ComputerName && sock.ConnectionType == ConnectionTypes.ClientConsole && sock.Partner == null))
                {
                    System.Threading.Thread.Sleep(200);
                    if (DateTime.Now - startTime > TimeSpan.FromSeconds(5))
                    {
                        JsonData.Status = "failed";
                        break;
                    }
                }
                if (JsonData.Status == "failed")
                {
                    Partner.Send(Json.Encode(JsonData));
                    Partner.Partner = null;
                    Partner = null;
                    Close();
                }
                else
                {
                    SocketCollection.Remove(this);
                    var newClient = SocketCollection.Find(sock => sock.ComputerName == ComputerName && sock.ConnectionType == ConnectionTypes.ClientConsole && sock.Partner == null);
                    newClient.Partner = Partner;
                    Partner.Partner = newClient;
                    JsonData.Status = "ok";
                    JsonData.ComputerName = newClient.ComputerName;
                    Partner.Send(Json.Encode(JsonData));
                    Partner = null;
                    Close();
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void HandleBounds(dynamic JsonData)
        {
            if (Config.Current.Session_Recording)
            {
                var videoFolder = Directory.CreateDirectory($@"{Utilities.App_Data}\Logs\Recordings\{Partner?.TechAccount?.UserID ?? Partner?.SessionID}\{DateTime.Now.Year.ToString()}\{DateTime.Now.Month.ToString().PadLeft(2, '0')}\{DateTime.Now.Day.ToString().PadLeft(2, '0')}\");
                var videoFile = Path.Combine(videoFolder.FullName, Partner?.SessionID + ".itr");
                File.AppendAllText(videoFile, $"{JsonData.Width},{JsonData.Height}{Environment.NewLine}");
            }
            Partner.Send(Json.Encode(JsonData));
        }
        public void HandleCtrlAltDel(dynamic JsonData)
        {
            try
            {
                Partner.Send(Json.Encode(JsonData));
                var service = SocketCollection.ToList().Find(rc => (rc as Remote_Control).ComputerName == Partner.ComputerName && (rc as Remote_Control).ConnectionType == ConnectionTypes.ClientService);
                if (service != null)
                {
                    service.Send(Json.Encode(JsonData));
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        private bool AuthenticateTech(dynamic JsonData)
        {
            try
            {
                if (!AuthenticationTokens.Contains(JsonData.AuthenticationToken))
                {
                    var response = new
                    {
                        Type = "Unauthorized",
                        Reason = "You are unauthorized to perform this action."
                    };
                    Send(Json.Encode(response));
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                return false;
            }
        }
        public void HandleFileDeploy(dynamic JsonData)
        {
            try
            {
                var sender = Socket_Main.SocketCollection.Find(sm => sm.ConnectionType == Socket_Main.ConnectionTypes.Technician && sm.TechAccount.UserID == JsonData.FromID);
                if (sender != null)
                {
                    sender.Send(Json.Encode(JsonData));
                    sender.LogFileDeployment(JsonData);
                }
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
                var sender = Socket_Main.SocketCollection.Find(sm => sm.ConnectionType == Socket_Main.ConnectionTypes.Technician && sm?.TechAccount?.UserID?.ToLower() == JsonData?.FromID?.ToLower());
                if (sender != null)
                {
                    sender.Send(Json.Encode(JsonData));
                    sender.LogConsoleCommand(JsonData);
                }
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
                var sender = Socket_Main.SocketCollection.Find(sm => sm.ConnectionType == Socket_Main.ConnectionTypes.Technician && sm?.TechAccount?.UserID?.ToLower() == JsonData?.FromID?.ToLower());
                if (sender != null)
                {
                    sender.Send(Json.Encode(JsonData));
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }

        }
        private void LogConnection()
        {
            try
            {
                var filePath = Path.Combine(Utilities.App_Data, "Logs", "Connections", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0') + ".txt");
                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                }
                var entry = new
                {
                    Timestamp = DateTime.Now.ToString(),
                    HostAddress = WebSocketContext.UserHostAddress,
                    UserAgent = WebSocketContext.UserAgent,
                    ConnectionType = ConnectionType.ToString(),
                    SessionID = SessionID
                };
                File.AppendAllText(filePath, Json.Encode(entry) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        private void LogSession()
        {
            try
            {
                var filePath = Path.Combine(Utilities.App_Data, "Logs", "Sessions", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0') + ".txt");
                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                }
                var entry = new
                {
                    Timestamp = DateTime.Now.ToString(),
                    SessionID = SessionID,
                    PartnerID = Partner?.SessionID
                };
                File.AppendAllText(filePath, Json.Encode(entry) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void Save()
        {
            try
            {
                if (String.IsNullOrWhiteSpace(ComputerName))
                {
                    return;
                }
                var di = Directory.CreateDirectory(Path.Combine(Utilities.App_Data, "Computer_Accounts"));
                var ca = new Computer_Account();
                ca.ComputerGroup = ComputerGroup;
                if (!String.IsNullOrWhiteSpace(CurrentUser))
                {
                    LastLoggedOnUser = CurrentUser;
                    ca.LastLoggedOnUser = CurrentUser;
                }
                File.WriteAllText(Path.Combine(di.FullName, ComputerName + ".json"), Json.Encode(ca));
                if (Config.Current.File_Encryption)
                {
                    try
                    {
                        File.Encrypt(Path.Combine(di.FullName, ComputerName + ".json"));
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        File.Decrypt(Path.Combine(di.FullName, ComputerName + ".json"));
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        public void Load()
        {
            try
            {
                var fi = new FileInfo(Path.Combine(Utilities.App_Data, "Computer_Accounts", ComputerName + ".json"));
                if (fi.Exists)
                {
                    var strCA = File.ReadAllText(fi.FullName);
                    var ca = Json.Decode<Computer_Account>(strCA);
                    ComputerGroup = ca?.ComputerGroup ?? "";
                    LastLoggedOnUser = ca?.LastLoggedOnUser ?? "";
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
    }
}