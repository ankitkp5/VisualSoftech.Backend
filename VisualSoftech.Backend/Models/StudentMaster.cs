namespace VisualSoftech.Backend.Models
{
    public class StudentMaster
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public DateTime Dob { get; set; }

        public string Address { get; set; }

        public int StateId { get; set; }

        public string PhoneNumber { get; set; }

        // Photos will be stored as JSON string in DB
        public string PhotosJson { get; set; }

        // Subjects list
        public List<StudentDetail> Subjects { get; set; }
    }
}
//using System;
//using System.Collections.Generic;


