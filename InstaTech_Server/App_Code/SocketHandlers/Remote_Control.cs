using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Web.WebSockets;
using System.Web.Helpers;

namespace InstaTech.App_Code.SocketHandlers
{
    public class Remote_Control : WebSocketHandler
    {
        private static WebSocketCollection socketCollection;

        public static WebSocketCollection SocketCollection
        {
            get
            {
                if (socketCollection == null)
                {
                    socketCollection = new WebSocketCollection();
                }
                return socketCollection;
            }
            set
            {
                socketCollection = value;
            }
        }
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
                case "ConnectionType":
                    {
                        ConnectionType = Enum.Parse(typeof(ConnectionTypes), jsonMessage.ConnectionType.ToString());
                        var random = new Random();
                        var sessionID = random.Next(0, 999).ToString().PadLeft(3, '0') + " " + random.Next(0, 999).ToString().PadLeft(3, '0');
                        SessionID = sessionID.Replace(" ", "");
                        var request = new
                        {
                            Type = "SessionID",
                            SessionID = sessionID
                        };
                        Send(Json.Encode(request));
                        logConnection();
                        break;
                    }
                case "Connect":
                    {
                        var client = SocketCollection.FirstOrDefault(sock => ((Remote_Control)sock).SessionID == jsonMessage.SessionID.ToString().Replace(" ", "") && ((Remote_Control)sock).ConnectionType == ConnectionTypes.ClientApp);
                        if (client != null)
                        {
                            if ((client as Remote_Control).Partner != null)
                            {
                                var request = new
                                {
                                    Type = "Connect",
                                    Status = "AlreadyHasPartner"
                                };
                                Send(Json.Encode(request));
                            }
                            else
                            {
                                this.Partner = (Remote_Control)client;
                                ((Remote_Control)client).Partner = this;
                                client.Send(message);
                                logSession();
                            }
                        }
                        else
                        {
                            var request = new
                            {
                                Type = "Connect",
                                Status = "InvalidID"
                            };
                            Send(Json.Encode(request));
                        }
                        break;
                    }
                default:
                    {
                        Partner.Send(message);
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
        private void logConnection()
        {
            if (!System.IO.Directory.Exists(WebSocketContext.Server.MapPath("/App_Data/Logs/")))
            {
                System.IO.Directory.CreateDirectory(WebSocketContext.Server.MapPath("/App_Data/Logs/"));
            }
            var strLogPath = WebSocketContext.Server.MapPath("/App_Data/Logs/Connections-") + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            System.IO.File.AppendAllText(strLogPath, DateTime.Now.ToString() + "\t" + WebSocketContext.UserHostAddress + "\t" + WebSocketContext.UserAgent + "\t" + SessionID);
        }
        private void logSession()
        {
            if (!System.IO.Directory.Exists(WebSocketContext.Server.MapPath("/App_Data/Logs/")))
            {
                System.IO.Directory.CreateDirectory(WebSocketContext.Server.MapPath("/App_Data/Logs/"));
            }
            var strLogPath = WebSocketContext.Server.MapPath("/App_Data/Logs/Sessions-") + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            System.IO.File.AppendAllText(strLogPath, DateTime.Now.ToString() + "\t" + SessionID + "\t" + Partner.SessionID);
        }
        public string SessionID { get; set; }
        public Remote_Control Partner { get; set; }
        public ConnectionTypes ConnectionType { get; set; }
        public enum ConnectionTypes
        {
            Customer,
            Technician,
            ClientApp,
            ViewerApp
        }
    }
}