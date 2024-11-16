using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.companyname.navigationgraph8net8.Classes
{
    internal class Book
    {
        public string? Title { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ReleaseDate { get; set; }
        public string Author => FirstName + " " + LastName;
        
        
        public Book(string title, string firstName, string lastName, string releasedate)
        {
            Title = title;
            FirstName = firstName;
            LastName = lastName;
            ReleaseDate = releasedate;
        }
    }
}
