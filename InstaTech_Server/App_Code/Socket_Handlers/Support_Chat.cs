using InstaTech.App_Code.Models;
using Microsoft.Web.WebSockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Helpers;

namespace InstaTech.App_Code.Socket_Handlers
{
    /// <summary>
    /// Summary description for Support_Chat
    /// </summary>
    public class Support_Chat : WebSocketHandler
    {
        public static WebSocketCollection SocketCollection { get; } = new WebSocketCollection();
        public static List<Case> Cases = new List<Case>();
        public Support_Chat()
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
            
            if (jsonMessage == null || String.IsNullOrEmpty(jsonMessage?.Type))
            {
                throw new Exception("Type is null within Support_Chat.OnMessage.");
            }
            var methodHandler = Type.GetType("InstaTech.App_Code.Socket_Handlers.Support_Chat").GetMethods().FirstOrDefault(mi => mi.Name == "Handle" + jsonMessage.Type);
            if (methodHandler != null)
            {
                methodHandler.Invoke(this, new object[] { jsonMessage });
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
            foreach (Support_Chat socket in SocketCollection)
            {
                socket.SendWaitUpdate();
            }
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
            foreach (Support_Chat socket in SocketCollection)
            {
                socket.SendWaitUpdate();
            }
        }
        public Support_Chat Partner { get; set; }
        public Case SupportCase { get; set; }
        public Tech_Account Tech { get; set; }
        public string AuthenticationToken { get; set; }
        public int BadLoginAttempts { get; set; } = 0;
        public ConnectionTypes? ConnectionType { get; set; }
        public enum ConnectionTypes
        {
            Customer,
            Technician
        }
        private int GetPlaceInQueue()
        {
            var cases = SocketCollection.Where(sock => (sock as Support_Chat).ConnectionType == ConnectionTypes.Customer && (sock as Support_Chat).Partner == null && (sock as Support_Chat)?.SupportCase?.DTCreated < SupportCase?.DTCreated);
            return cases.Count() + 1;
        }
        private bool AuthenticateTech(dynamic JsonData)
        {
            if (JsonData.AuthToken != AuthenticationToken)
            {
                var response = new
                {
                    Type = "Kicked",
                    Reason = "Unauthorized request."
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
        public void HandleCustomerLogin(dynamic JsonData)
        {
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
                SupportType = JsonData.SupportType
            };
            ConnectionType = ConnectionTypes.Customer;
            SupportCase.Save();
            JsonData.Status = "ok";
            JsonData.Place = GetPlaceInQueue();
            Send(Json.Encode(JsonData));
        }
        
        public void HandleGetSupportCategories(dynamic JsonData)
        {
            var categories = new List<string>();
            foreach (var tuple in Config.Support_Categories)
            {
                categories.Add(tuple.Item1);
            }
            JsonData.Categories = categories.Distinct();
            Send(Json.Encode(JsonData));
        }
        public void HandleGetSupportTypes(dynamic JsonData)
        {
            var tuples = Config.Support_Categories.FindAll(tp => tp.Item1 == JsonData.SupportCategory);
            var types = new List<string>();
            foreach (var tuple in tuples)
            {
                types.Add(tuple.Item2);
            }
            JsonData.Types = types;
            Send(Json.Encode(JsonData));
        }
        public void HandleTechLogin(dynamic JsonData)
        {
            if (ConnectionType != null)
            {
                return;
            }
            if (BadLoginAttempts >= 3)
            {
                JsonData.Status = "temp ban";
                Send(Json.Encode(JsonData));
                return;
            }
            if (Config.Demo_Mode && JsonData.UserID.ToLower() == "demo" && JsonData.Password == "tech")
            {
                ConnectionType = ConnectionTypes.Technician;
                JsonData.Status = "ok";
                AuthenticationToken = Guid.NewGuid().ToString().Replace("-", "");
                JsonData.AuthenticationToken = AuthenticationToken;
                Send(Json.Encode(JsonData));
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
                if (!File.Exists(Utilities.App_Data + "Tech_Accounts\\" + JsonData.UserID + ".json"))
                {
                    BadLoginAttempts++;
                    JsonData.Status = "invalid";
                    Send(Json.Encode(JsonData));
                    return;
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
                        return;
                    }
                }
                if (JsonData.Password == account.TempPassword)
                {
                    if (String.IsNullOrEmpty(JsonData.NewPassword))
                    {
                        JsonData.Status = "new required";
                        Send(Json.Encode(JsonData));
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
                        account.TempPassword = null;
                        account.HashedPassword = Crypto.HashPassword(JsonData.ConfirmNewPassword);
                        account.BadLoginAttempts = 0;
                        account.Save();
                        JsonData.Status = "ok";
                        AuthenticationToken = Guid.NewGuid().ToString().Replace("-", "");
                        JsonData.AuthenticationToken = AuthenticationToken;
                        Send(Json.Encode(JsonData));
                        return;
                    }
                    return;
                }
                if (Crypto.VerifyHashedPassword(account.HashedPassword, JsonData.Password))
                {
                    ConnectionType = ConnectionTypes.Technician;
                    account.BadLoginAttempts = 0;
                    account.TempPassword = null;
                    account.Save();
                    JsonData.Status = "ok";
                    AuthenticationToken = Guid.NewGuid().ToString().Replace("-", "");
                    JsonData.AuthenticationToken = AuthenticationToken;
                    Send(Json.Encode(JsonData));
                    return;
                }
                else
                {
                    BadLoginAttempts++;
                    account.BadLoginAttempts++;
                    account.LastBadLogin = DateTime.Now;
                    account.Save();
                    JsonData.Status = "invalid";
                    Send(Json.Encode(JsonData));
                    return;
                }
            }
        }
        public void HandleGetQueues(dynamic JsonData)
        {
            if (!AuthenticateTech(JsonData))
            {
                return;
            }
            var queues = new List<string>();
            foreach (var tuple in Config.Support_Categories)
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
    }
}
