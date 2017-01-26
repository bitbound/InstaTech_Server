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
            ClientConsole
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

            if (jsonMessage == null || String.IsNullOrEmpty(jsonMessage.Type))
            {
                throw new Exception("Type is null within Remote_Control.OnMessage.");
            }

            switch (jsonMessage.Type as String)
            {
                case "TechMainLogin":
                    {
                        if (BadLoginAttempts >= 3)
                        {
                            jsonMessage.Status = "temp ban";
                            Send(Json.Encode(jsonMessage));
                            return;
                        }
                        if (Config.Demo_Mode && jsonMessage.UserID.ToLower() == "demo" && jsonMessage.Password == "tech")
                        {
                            AuthenticationToken = Guid.NewGuid().ToString().Replace("-", "");
                            TechAccount = new Tech_Account()
                            {
                                UserID = "demo",
                                FirstName = "Demo",
                                LastName = "Tech",
                                HashedPassword = Crypto.HashPassword(jsonMessage.Password)
                            };
                            if (jsonMessage.RememberMe == true)
                            {
                                TechAccount.AuthenticationToken = AuthenticationToken;
                            }
                            else
                            {
                                TechAccount.AuthenticationToken = null;
                            }
                            TechAccount.Save();
                            jsonMessage.Status = "ok";
                            jsonMessage.AuthenticationToken = AuthenticationToken;
                            Send(Json.Encode(jsonMessage));
                            return;
                        }
                        else if (Config.Active_Directory_Enabled)
                        {
                            // TODO: AD authentication.
                        }
                        else
                        {
                            if (!Directory.Exists(Utilities.App_Data + "Tech_Accounts"))
                            {
                                Directory.CreateDirectory(Utilities.App_Data + "Tech_Accounts");
                            }
                            if (!File.Exists(Utilities.App_Data + "Tech_Accounts\\" + jsonMessage.UserID + ".json"))
                            {
                                BadLoginAttempts++;
                                jsonMessage.Status = "invalid";
                                Send(Json.Encode(jsonMessage));
                                return;
                            }
                            Tech_Account account = Json.Decode<Tech_Account>(File.ReadAllText(Utilities.App_Data + "Tech_Accounts\\" + jsonMessage.UserID + ".json"));
                            if (account.BadLoginAttempts >= 3)
                            {
                                if (DateTime.Now - account.LastBadLogin > TimeSpan.FromMinutes(10))
                                {
                                    BadLoginAttempts = 0;
                                }
                                else
                                {
                                    jsonMessage.Status = "locked";
                                    Send(Json.Encode(jsonMessage));
                                    return;
                                }
                            }
                            if (jsonMessage.Password == account.TempPassword)
                            {
                                if (String.IsNullOrEmpty(jsonMessage.NewPassword))
                                {
                                    jsonMessage.Status = "new required";
                                    Send(Json.Encode(jsonMessage));
                                    return;
                                }
                                else if (jsonMessage.NewPassword != jsonMessage.ConfirmNewPassword)
                                {
                                    jsonMessage.Status = "password mismatch";
                                    Send(Json.Encode(jsonMessage));
                                    return;
                                }
                                else if (jsonMessage.NewPassword.Length < 8 || jsonMessage.NewPassword.Length > 20)
                                {
                                    jsonMessage.Status = "password length";
                                    Send(Json.Encode(jsonMessage));
                                    return;
                                }
                                else
                                {
                                    AuthenticationToken = Guid.NewGuid().ToString().Replace("-", "");
                                    account.TempPassword = null;
                                    account.HashedPassword = Crypto.HashPassword(jsonMessage.ConfirmNewPassword);
                                    account.BadLoginAttempts = 0;
                                    if (jsonMessage.RememberMe == true)
                                    {
                                        account.AuthenticationToken = AuthenticationToken;
                                    }
                                    else
                                    {
                                        account.AuthenticationToken = null;
                                    }
                                    account.Save();
                                    jsonMessage.Status = "ok";
                                    jsonMessage.AuthenticationToken = AuthenticationToken;
                                    Send(Json.Encode(jsonMessage));
                                    return;
                                }
                            }
                            if (Crypto.VerifyHashedPassword(account.HashedPassword, jsonMessage.Password))
                            {
                                AuthenticationToken = Guid.NewGuid().ToString().Replace("-", "");
                                account.BadLoginAttempts = 0;
                                account.TempPassword = null;
                                if (jsonMessage.RememberMe == true)
                                {
                                    account.AuthenticationToken = AuthenticationToken;
                                }
                                else
                                {
                                    account.AuthenticationToken = null;
                                }
                                account.Save();
                                TechAccount = account;
                                jsonMessage.Status = "ok";
                                jsonMessage.AuthenticationToken = AuthenticationToken;
                                Send(Json.Encode(jsonMessage));
                                return;
                            }
                            if (!String.IsNullOrEmpty(jsonMessage.AuthenticationToken))
                            {
                                if (jsonMessage.AuthenticationToken == AuthenticationToken || jsonMessage.AuthenticationToken == account.AuthenticationToken)
                                {
                                    AuthenticationToken = jsonMessage.AuthenticationToken;
                                    account.BadLoginAttempts = 0;
                                    account.TempPassword = null;
                                    account.Save();
                                    TechAccount = account;
                                    jsonMessage.Status = "ok";
                                    Send(Json.Encode(jsonMessage));
                                }
                                else
                                {
                                    BadLoginAttempts++;
                                    jsonMessage.Status = "invalid";
                                    Send(Json.Encode(jsonMessage));
                                }
                                return;
                            }
                            // Bad login attempt.
                            BadLoginAttempts++;
                            account.BadLoginAttempts++;
                            account.LastBadLogin = DateTime.Now;
                            account.Save();
                            jsonMessage.Status = "invalid";
                            Send(Json.Encode(jsonMessage));
                            return;
                        }
                        break;
                    }
                case "ConnectionType":
                    {
                        ConnectionType = Enum.Parse(typeof(ConnectionTypes), jsonMessage.ConnectionType.ToString());
                        if (ConnectionType == ConnectionTypes.ClientApp || ConnectionType == ConnectionTypes.ViewerApp)
                        {
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
                            var client = SocketCollection.FirstOrDefault(sock => (sock as Remote_Control).ComputerName == jsonMessage.ComputerName);
                            if (client != null)
                            {
                                client.Close();
                            }
                            ComputerName = jsonMessage.ComputerName.ToString().Trim().ToLower();
                        }
                        else if (ConnectionType == ConnectionTypes.ClientService)
                        {
                            var client = SocketCollection.FirstOrDefault(sock => (sock as Remote_Control).ComputerName == jsonMessage.ComputerName);
                            if (client != null)
                            {
                                jsonMessage.Status = "ServiceRunning";
                                Send(Json.Encode(jsonMessage));
                            }
                        }
                        LogConnection();
                        break;
                    }
                case "SearchComputers":
                    {
                        if (!AuthenticateTech(jsonMessage))
                        {
                            return;
                        }
                        var computers = new List<string>();
                        var clients = SocketCollection.Cast<Remote_Control>().ToList().FindAll(rc => rc.ConnectionType == ConnectionTypes.ClientService);
                        foreach (var client in clients)
                        {
                            computers.Add(client.ComputerName);
                        }
                        jsonMessage.Computers = computers;
                        Send(Json.Encode(jsonMessage));
                        break;
                    }
                case "Connect":
                    {
                        var client = SocketCollection.FirstOrDefault(sock => ((Remote_Control)sock).SessionID == jsonMessage.SessionID.ToString().Replace(" ", "") && ((Remote_Control)sock).ConnectionType == ConnectionTypes.ClientApp);
                        if (client != null)
                        {
                            if ((client as Remote_Control).Partner != null)
                            {
                                jsonMessage.Status = "AlreadyHasPartner";
                                Send(Json.Encode(jsonMessage));
                            }
                            else
                            {
                                jsonMessage.Status = "ok";
                                Send(Json.Encode(jsonMessage));
                                this.Partner = (Remote_Control)client;
                                ((Remote_Control)client).Partner = this;
                                client.Send(message);
                                LogSession();
                            }
                        }
                        else
                        {
                            jsonMessage.Status = "InvalidID";
                            Send(Json.Encode(jsonMessage));
                        }
                        break;
                    }
                case "ConnectUnattended":
                    if (!AuthenticateTech(jsonMessage))
                    {
                        return;
                    }
                    var consoleClient = (Remote_Control)SocketCollection.FirstOrDefault(sock => ((Remote_Control)sock).ComputerName == jsonMessage.ComputerName.ToString().Trim().ToLower() && ((Remote_Control)sock).ConnectionType == ConnectionTypes.ClientConsole);
                    if (consoleClient != null)
                    {
                        if (consoleClient.Partner != null)
                        {
                            jsonMessage.Status = "AlreadyHasPartner";
                            Send(Json.Encode(jsonMessage));
                        }
                        else
                        {
                            jsonMessage.Status = "ok";
                            Send(Json.Encode(jsonMessage));
                            this.Partner = consoleClient;
                            consoleClient.Partner = this;
                            consoleClient.Send(message);
                            LogSession();
                        }
                    }
                    else
                    {
                        jsonMessage.Status = "UnknownComputer";
                        Send(Json.Encode(jsonMessage));
                    }
                    var serviceClient = (Remote_Control)SocketCollection.FirstOrDefault(sock => ((Remote_Control)sock).ComputerName == jsonMessage.ComputerName.ToString().Trim() && ((Remote_Control)sock).ConnectionType == ConnectionTypes.ClientService);
                    break;
                default:
                    {
                        if (Partner != null)
                        {
                            Partner.Send(message);
                        }
                        break;
                    }
            }
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
            SocketCollection.Remove(this);
        }
        private bool AuthenticateTech(dynamic JsonData)
        {
            if (TechAccount?.UserID.ToLower() == "demo" && JsonData.Type == "ConnectUnattended")
            {
                var response = new
                {
                    Type = "Unauthorized",
                    Reason = "The demo account is not authorized to start unattended sessions."
                };
                Send(Json.Encode(response));
                return false;
            }
            if (TechAccount?.UserID.ToLower() == "demo" && JsonData.Type == "SearchComputers")
            {
                var response = new
                {
                    Type = "Unauthorized",
                    Reason = "The demo account is not authorized to search computers."
                };
                Send(Json.Encode(response));
                return false;
            }
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
            if (!System.IO.Directory.Exists(Utilities.App_Data + "Logs"))
            {
                System.IO.Directory.CreateDirectory(Utilities.App_Data + "Logs");
            }
            var strLogPath = Utilities.App_Data + "Logs\\Connections-" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            System.IO.File.AppendAllText(strLogPath, DateTime.Now.ToString() + "\t" + WebSocketContext.UserHostAddress + "\t" + WebSocketContext.UserAgent + "\t" + ConnectionType.ToString() + "\t" + SessionID);
        }
        private void LogSession()
        {
            if (!System.IO.Directory.Exists(Utilities.App_Data + "Logs"))
            {
                System.IO.Directory.CreateDirectory(Utilities.App_Data + "Logs");
            }
            var strLogPath = Utilities.App_Data + "Logs\\Sessions-" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            System.IO.File.AppendAllText(strLogPath, DateTime.Now.ToString() + "\t" + SessionID + "\t" + Partner.SessionID);
        }
    }
}