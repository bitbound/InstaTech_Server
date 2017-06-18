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
            var year = DateTime.Now.Year.ToString();
            var month = DateTime.Now.Month.ToString().PadLeft(2, '0');
            var day = DateTime.Now.Day.ToString().PadLeft(2, '0');

            var baseDir = Directory.CreateDirectory(Path.Combine(Utilities.App_Data, "Cases", year, month, day));
            var count = 0;
            while (File.Exists(Path.Combine(baseDir.FullName, year + month + day + count.ToString() + ".json")))
            {
                count++;
            }
            CaseID = year + month + day + count.ToString();
            DTCreated = DateTime.Now;
            File.Create(Path.Combine(baseDir.FullName, CaseID + ".json")).Close();
        }
        public string CaseID { get; set; } = "";
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
                return Config.Current.Support_Categories.Find(sc => sc.Category == SupportCategory && sc.Type == SupportType).Queue;
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
        public enum CaseStatus
        {
            Open,
            Resolved,
            Abandoned,
            Unresolved
        }
        public void Save()
        {
            var path = Path.Combine(Utilities.App_Data, "Cases", CaseID.Substring(0, 4), CaseID.Substring(4, 2), CaseID.Substring(6, 2), CaseID + ".json");

            File.WriteAllText(path, Json.Encode(this));
            if (Config.Current.File_Encryption)
            {
                // TODO: Encryption fails when account doesn't have write access to entire path.  Find workaround.
                try
                {
                    File.Encrypt(path);
                }
                catch { }
            }
            else
            {
                try
                {
                    File.Decrypt(path);
                }
                catch { }
            }
        }
        public static Case Load (string CaseID)
        {
            var path = Path.Combine(Utilities.App_Data, "Cases", CaseID.Substring(0, 4), CaseID.Substring(4, 2), CaseID.Substring(6, 2), CaseID + ".json");
            var strCase = File.ReadAllText(path);
            return Json.Decode<Case>(strCase);
        }
    }
}