using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using InstaTech.App_Code;
using System.Web.Helpers;
using System.Timers;

namespace InstaTech.App_Code.Models
{
    /// <summary>
    /// Summary description for Case
    /// </summary>
    public class Case
    {
        public Case()
        {
            var dirs = Directory.CreateDirectory(Utilities.App_Data + "Cases\\").GetDirectories().ToList();
            if (dirs.Count > 0)
            {
                dirs.Sort();
                CaseID = int.Parse(dirs.Last().Name.Split('-')[0]);
            }
            while (Directory.GetFiles(CaseDir).Length == 999)
            {
                CaseID += 1000;
            }
            while (File.Exists(CaseDir + CaseID + ".json"))
            {
                CaseID += 1;
            }
            DTCreated = DateTime.Now;
        }
        public int CaseID { get; set; } = 1;
        public DateTime? DTCreated { get; set; }
        public DateTime? DTReceived { get; set; }
        public DateTime? DTClosed { get; set; }
        public CaseStatus Status { get; set; } = CaseStatus.Open;
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; }
        public string CustomerUserID { get; set; }
        public string CustomerComputerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string SupportCategory { get; set; }
        public string SupportType { get; set; }
        public string SupportQueue
        {
            get
            {
                return Config.Current.Support_Categories.Find(tp => tp.Item1 == SupportCategory && tp.Item2 == SupportType).Item3;
            }
        }
        public string Details { get; set; }
        public string TechUserID { get; set; }
        public bool Locked
        {
            get
            {
                if (LockedAt == null || DateTime.Now - LockedAt > TimeSpan.FromSeconds(20))
                {
                    LockedAt = null;
                    return false;
                }
                else
                {
                    return true;
                }
            }
            set
            {
                if (value == true)
                {
                    LockedAt = DateTime.Now;
                }
                else
                {
                    LockedAt = null;
                }
            }
        }
        public string LockedBy { get; set; }
        private DateTime? LockedAt { get; set; }
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        private string CaseDir
        {
            get
            {
                var floored = Math.Floor((decimal)(CaseID / 1000));
                if (!Directory.Exists(Utilities.App_Data + "Cases\\" + String.Format("{0}-{1}", floored + 1, floored + 1000)))
                {
                    Directory.CreateDirectory(Utilities.App_Data + "Cases\\" + String.Format("{0}-{1}", floored + 1, floored + 1000));
                }
                return Utilities.App_Data + "Cases\\" + String.Format("{0}-{1}", floored + 1, floored + 1000) + "\\";
            }
        }
        public enum CaseStatus
        {
            Open,
            Resolved,
            Abandoned,
            Unresolved
        }
        public void Save()
        {
            File.WriteAllText(CaseDir + CaseID + ".json", Json.Encode(this));
        }
    }
}