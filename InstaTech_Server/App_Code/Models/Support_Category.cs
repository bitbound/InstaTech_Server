using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class Support_Category
{
    public Support_Category()
    {
    
    }
    public Support_Category(string Category, string Type, string Queue)
    {
        this.Category = Category;
        this.Type = Type;
        this.Queue = Queue;
    }
    public string Category { get; set; }
    public string Type { get; set; }
    public string Queue { get; set; }
}