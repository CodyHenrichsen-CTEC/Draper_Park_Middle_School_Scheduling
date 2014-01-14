using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Draper_Park_Scheduler
{
    class Student
    {
        private String firstName, lastName, teamName, homeRoomTeacher,  gradeLevel,  sessionOne, sessionTwo, sessionThree;
        private Request studentRequest;
        private bool okToSchedule;

        
        public String FirstName
        {
            get { return firstName; }
            set { firstName = value; }
        }

        public String LastName
        {
            get { return lastName; }
            set { lastName = value; }
        }
        
        public String TeamName
        {
            get { return teamName; }
            set { teamName = value; }
        }

        public String HomeRoomTeacher
        {
            get { return homeRoomTeacher; }
            set { homeRoomTeacher = value; }
        }

        public String GradeLevel
        {
            get { return gradeLevel; }
            set { gradeLevel = value; }
        }

        public String SessionOne
        {
            get { return sessionOne; }
            set { sessionOne = value; }
        }

        public String SessionTwo
        {
            get { return sessionTwo; }
            set { sessionTwo = value; }
        }

        public String SessionThree
        {
            get { return sessionThree; }
            set { sessionThree = value; }
        }

        public Request StudentRequest
        {
            get { return studentRequest; }
            set { studentRequest = value; }
        }

       
        
        public Student(String firstName, String lastName, String studentNumber,  String studentTeacher, String gradeLevel)
        {
            this.firstName = firstName;
            this.lastName = lastName;
            this.teamName = studentTeacher;
            this.homeRoomTeacher = studentNumber;
            this.gradeLevel = gradeLevel;
            

            sessionOne = "";
            sessionTwo = "";
            sessionThree = "";
        }
    }
}
