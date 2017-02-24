using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Web.WebSockets;
using System.Web.Helpers;
using InstaTech.App_Code.Models;
using System.IO;

namespace InstaTech.App_Code.Socket_Handlers
{
    public class Remote_Control : WebSocketHandler
    {
        
        #region User-Defined Properties.
        public static WebSocketCollection SocketCollection { get; set; } = new WebSocketCollection();
        public string SessionID { get; set; }
        public Remote_Control Partner { get; set; }
        public string ComputerName { get; set; }
        public string AuthenticationToken { get; set; }
        public int BadLoginAttempts { get; set; } = 0;
        public Tech_Account TechAccount { get; set; }
        public ConnectionTypes? ConnectionType { get; set; }
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
            SocketCollection.Add(this);
        }
        public override void OnClose()
        {
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
            SocketCollection.Remove(this);
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
                    Type = "PartnerError"
                };
                Partner.Send(Json.Encode(request));
                Partner.Close();
                Partner.Partner = null;
                Partner = null;
            }
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
                throw new Exception("Type is null within Remote_Control.OnMessage.");
            }
            var methodHandler = Type.GetType("InstaTech.App_Code.Socket_Handlers.Remote_Control").GetMethods().FirstOrDefault(mi => mi.Name == "Handle" + jsonMessage.Type);
            if (methodHandler != null)
            {
                try
                {
                    methodHandler.Invoke(this, new object[] { jsonMessage });
                }
                catch (Exception ex)
                {
                    var filePath = Path.Combine(Utilities.App_Data, "Errors", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0') + ".txt");
                    if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
                    {
                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
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
            else
            {
                if (Partner != null)
                {
                    Partner.Send(message);
                }
            }
        }
        public void HandleTechMainLogin(dynamic JsonData) {
            if (BadLoginAttempts >= 3)
            {
                JsonData.Status = "temp ban";
                Send(Json.Encode(JsonData));
                return;
            }
            if (Config.Current.Demo_Mode && JsonData.UserID.ToLower() == "demo" && JsonData.Password == "tech")
            {
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
                JsonData.AuthenticationToken = AuthenticationToken;
                Send(Json.Encode(JsonData));
                return;
            }
            else if (Config.Current.Active_Directory_Enabled)
            {
                // TODO: AD authentication.
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
                    return;
                }
                Tech_Account account  = Json.Decode<Tech_Account>(File.ReadAllText(Utilities.App_Data + "Tech_Accounts\\" + JsonData.UserID + ".json"));
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
                        return;
                    }
                }
                if (Crypto.VerifyHashedPassword(account.HashedPassword, JsonData.Password))
                {
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
                    return;
                }
                if (!String.IsNullOrEmpty(JsonData.AuthenticationToken))
                {
                    if (JsonData.AuthenticationToken == AuthenticationToken || JsonData.AuthenticationToken == account.AuthenticationToken)
                    {
                        AuthenticationToken = JsonData.AuthenticationToken;
                        account.BadLoginAttempts = 0;
                        account.TempPassword = null;
                        account.Save();
                        TechAccount = account;
                        JsonData.Status = "ok";
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
        public void HandleForgotPassword(dynamic JsonData) {
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
        public void HandleConnectionType (dynamic JsonData)
        {
            ConnectionType = Enum.Parse(typeof(ConnectionTypes), JsonData.ConnectionType.ToString());
            if (ConnectionType == ConnectionTypes.ClientApp || ConnectionType == ConnectionTypes.ViewerApp)
            {
                if (!String.IsNullOrWhiteSpace(JsonData.ComputerName))
                {
                    ComputerName = JsonData.ComputerName.ToString().Trim().ToLower();
                }
                var random = new Random();
                var sessionID = random.Next(0, 999).ToString().PadLeft(3, '0') + " " + random.Next(0, 999).ToString().PadLeft(3, '0');
                SessionID = sessionID.Replace(" ", "");
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
                var client = SocketCollection.FirstOrDefault(sock => (sock as Remote_Control).ComputerName == JsonData.ComputerName);
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
                        Timestamp = DateTime.Now,
                        Message = "The service may be running on two computers that have the same name.",
                        ComputerName = JsonData.ComputerName.ToString().Trim(),
                        IPAddress = WebSocketContext.UserHostAddress
                    };
                    File.AppendAllText(filePath, Json.Encode(entry) + Environment.NewLine);
                }
                ComputerName = JsonData.ComputerName.ToString().Trim().ToLower();
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
        public void HandleSearchComputers(dynamic JsonData) {
            if (!AuthenticateTech(JsonData))
            {
                return;
            }
            var computers = new List<string>();
            var clients = SocketCollection.Cast<Remote_Control>().ToList().FindAll(rc => rc.ConnectionType == ConnectionTypes.ClientService);
            foreach (var client in clients)
            {
                computers.Add(client.ComputerName);
            }
            JsonData.Computers = computers.Distinct();
            Send(Json.Encode(JsonData));
        }
        public void HandleConnect(dynamic JsonData)
        {
            var client = SocketCollection.FirstOrDefault(sock => ((Remote_Control)sock).SessionID == JsonData.SessionID.ToString().Replace(" ", "") && ((Remote_Control)sock).ConnectionType == ConnectionTypes.ClientApp);
            if (client != null)
            {
                if ((client as Remote_Control).Partner != null)
                {
                    JsonData.Status = "AlreadyHasPartner";
                    Send(Json.Encode(JsonData));
                }
                else
                {
                    this.Partner = (Remote_Control)client;
                    ((Remote_Control)client).Partner = this;
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
        public void HandleConnectUnattended(dynamic JsonData)
        {
            var consoleClient = (Remote_Control)SocketCollection.FirstOrDefault(sock => ((Remote_Control)sock).ComputerName == JsonData.ComputerName.ToString().Trim().ToLower() && ((Remote_Control)sock).ConnectionType == ConnectionTypes.ClientConsole);
            if (consoleClient != null)
            {
                if (consoleClient.Partner != null)
                {
                    JsonData.Status = "AlreadyHasPartner";
                    Send(Json.Encode(JsonData));
                    return;
                }
            }
            var serviceClient = (Remote_Control)SocketCollection.FirstOrDefault(sock => ((Remote_Control)sock).ComputerName == JsonData.ComputerName.ToString().Trim().ToLower() && ((Remote_Control)sock).ConnectionType == ConnectionTypes.ClientService);
            if (serviceClient != null)
            {
                if (serviceClient.Partner != null)
                {
                    serviceClient.Partner.Close();
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
        public void HandleConnectUpgrade(dynamic JsonData)
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
                serviceClient = (Remote_Control)SocketCollection.FirstOrDefault(sock => ((Remote_Control)sock).ComputerName == JsonData.ComputerName.ToString().Trim().ToLower() && ((Remote_Control)sock).ConnectionType == ConnectionTypes.ClientServiceOnce);
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
                    consoleClient = (Remote_Control)SocketCollection.FirstOrDefault(sock => ((Remote_Control)sock).ComputerName == JsonData.ComputerName.ToString().Trim().ToLower() && ((Remote_Control)sock).ConnectionType == ConnectionTypes.ClientConsoleOnce);
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
        public void HandleProcessStartResult(dynamic JsonData)
        {
            var partner = Partner;
            Partner.Partner = null;
            Partner = null;
            if (JsonData.Status == "ok")
            {
                var started = DateTime.Now;
                var success = true;
                while (SocketCollection.Where(sock => ((Remote_Control)sock).ComputerName == ComputerName && ((Remote_Control)sock).ConnectionType == ConnectionTypes.ClientConsole).Count() == 0)
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
        public void HandleCompleteConnection(dynamic JsonData)
        {
            if (!AuthenticateTech(JsonData))
            {
                return;
            }
            var consoleClient = (Remote_Control)SocketCollection.FirstOrDefault(sock => ((Remote_Control)sock).ComputerName == JsonData.ComputerName.ToString().Trim().ToLower() && ((Remote_Control)sock).ConnectionType == ConnectionTypes.ClientConsole);
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
        public void HandleDesktopSwitch(dynamic JsonData)
        {
            Partner.Send(Json.Encode(JsonData));
            var startTime = DateTime.Now;
            JsonData.Status = "ok";
            while (!SocketCollection.Cast<Remote_Control>().ToList().Exists(sock => sock.ComputerName == ComputerName && sock.ConnectionType == ConnectionTypes.ClientConsole && sock.Partner == null))
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
                var newClient = (Remote_Control)SocketCollection.FirstOrDefault(sock => ((Remote_Control)sock).ComputerName == ComputerName && ((Remote_Control)sock).ConnectionType == ConnectionTypes.ClientConsole && (sock as Remote_Control).Partner == null);
                newClient.Partner = Partner;
                Partner.Partner = newClient;
                JsonData.Status = "ok";
                JsonData.ComputerName = newClient.ComputerName;
                Partner.Send(Json.Encode(JsonData));
                Partner = null;
                Close();
            }
        }
        public void HandleCtrlAltDel(dynamic JsonData)
        {
            Partner.Send(Json.Encode(JsonData));
            var service = SocketCollection.ToList().Find(rc => (rc as Remote_Control).ComputerName == Partner.ComputerName && (rc as Remote_Control).ConnectionType == ConnectionTypes.ClientService);
            if (service != null)
            {
                service.Send(Json.Encode(JsonData));
            }
        }
        private bool AuthenticateTech(dynamic JsonData)
        {
            if (JsonData.AuthenticationToken != AuthenticationToken)
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
        private void LogConnection()
        {
            var filePath = Path.Combine(Utilities.App_Data, "Logs", "Connections", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0') + ".txt");
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            var entry = new
            {
                Timestamp = DateTime.Now,
                HostAddress = WebSocketContext.UserHostAddress,
                UserAgent = WebSocketContext.UserAgent,
                ConnectionType = ConnectionType.ToString(),
                SessionID = SessionID
            };
            File.AppendAllText(filePath, Json.Encode(entry) + Environment.NewLine);
        }
        private void LogSession()
        {
            var filePath = Path.Combine(Utilities.App_Data, "Logs", "Sessions", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0') + ".txt");
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            var entry = new
            {
                Timestamp = DateTime.Now,
                SessionID = SessionID,
                PartnerID = Partner?.SessionID
            };
            File.AppendAllText(filePath, Json.Encode(entry) + Environment.NewLine);
        }
    }
}