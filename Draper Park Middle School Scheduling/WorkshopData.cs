using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OfficeOpenXml;
using System.Data;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;

namespace Draper_Park_Scheduler
{
    class WorkshopData
    {
        
        private string requestFilePath, workshopInfoFilePath, statusText, failedCutoff, exceedsCount, exceedsMessage;
        private DateTime cutoffTime;
        //private DataTable roomTable, schoolTable, districtTable, presenterTable, requestTable, studentTable, sessionTable;
        private Dictionary<String, Int32> AllClasses, ThirdClasses, SecondClasses, FirstClasses;
        private List <DistrictClass> districtClassCount;
        private List<Session> stillOneBlank, stillTwoBlank, stillThreeBlank;
        private List<DistrictClass> stupidList;

        private bool okToSchedule;

        private List <Room> roomList;
        private List <School> schoolList;
        private List <District> districtList;
        private List <Presenter> presenterList; 
        private List <Request> requestList;
        private List <Student> studentList;
        private List <Session> sessionList;

        public List<District> DistrictList
        {
            get { return districtList; }
            set { districtList = value; }
        }

        public bool ScheduleOK
        {
            get { return okToSchedule; }
            set { okToSchedule = value; }
        }

        public DateTime CutoffTime
        {
            get { return cutoffTime; }
            set { cutoffTime = value; }
        }

        public string RequestFilePath
        {
            get { return requestFilePath; }
            set { requestFilePath = value; }
        }

        public string WorkshopInfoFilePath
        {
            get { return workshopInfoFilePath; }
            set { workshopInfoFilePath = value; }
        }

        public WorkshopData()
        {
           
            okToSchedule = false;
            statusText = "Creating Workshop data: ";
            failedCutoff = "No sessions, enrolled after cutoff time";
            exceedsCount = "District has exceeded registration capability, student not scheduled";
        }

        public string StatusText
        {
            get { return statusText; }
        }

        /// <summary>
        /// Determine popularity of classes for the selection process using the lists
        /// </summary>
        public void verifyPopularity()
        {
            AllClasses = new Dictionary<String, int>();
           
            ThirdClasses = new Dictionary<String, int>();
            SecondClasses = new Dictionary<String, int>();
            FirstClasses = new Dictionary<String, int>();

            foreach(Presenter currentPresenter in presenterList)
            {
                AllClasses.Add(currentPresenter.PresenterTitle, 0);

                #region Loop over all requests and populate dictionaries
                foreach (Request currentRequest in requestList)
                {
                    string presenterName = currentPresenter.PresenterTitle;
                    if (currentRequest.RequestOne.Equals(presenterName) || currentRequest.RequestTwo.Equals(presenterName) || currentRequest.RequestThree.Equals(presenterName) || currentRequest.RequestFour.Equals(presenterName) || currentRequest.RequestFive.Equals(presenterName))
                    {
                        if (AllClasses.ContainsKey(presenterName))
                        {
                            int currentCount = AllClasses[presenterName];
                            ++currentCount;
                            AllClasses[presenterName] = currentCount;
                        }
                        else
                        {
                            AllClasses.Add(presenterName, 1);
                        }
                    }

                    

                    if (currentRequest.RequestThree.Equals(presenterName) )
                    {
                        if (ThirdClasses.ContainsKey(presenterName))
                        {
                            int currentCount = ThirdClasses[presenterName];
                            ++currentCount;
                            ThirdClasses[presenterName] = currentCount;
                        }
                        else
                        {
                            ThirdClasses.Add(presenterName, 1);
                        }
                    }

                    if (currentRequest.RequestTwo.Equals(presenterName) )
                    {
                        if (SecondClasses.ContainsKey(presenterName))
                        {
                            int currentCount = SecondClasses[presenterName];
                            ++currentCount;
                            SecondClasses[presenterName] = currentCount;
                        }
                        else
                        {
                            SecondClasses.Add(presenterName, 1);
                        }
                    }

                    if (currentRequest.RequestOne.Equals(presenterName))
                    {
                        if (FirstClasses.ContainsKey(presenterName))
                        {
                            int currentCount = FirstClasses[presenterName];
                            ++currentCount;
                            FirstClasses[presenterName] = currentCount;
                        }
                        else
                        {
                            FirstClasses.Add(presenterName, 1);
                        }
                    }
                }
            #endregion
            }

            AllClasses = AllClasses.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            ThirdClasses = ThirdClasses.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            SecondClasses = SecondClasses.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            FirstClasses = FirstClasses.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

        }

        private bool checkSheetsMatch(ExcelWorkbook current)
        {
            bool sheetsMatch = false;
            int rooms = 0, presenters = 0;

            for (int count = 0; count < 2; count++)
            {
                ExcelWorksheet currentWorksheet = current.Worksheets[count + 1];
                if (currentWorksheet.Name.Equals("Rooms"))
                {
                    rooms = 1;
                }
                if (currentWorksheet.Name.Equals("Presenters"))
                {
                    presenters = 1;
                }
            }
            sheetsMatch = (rooms + presenters == 2);
            return sheetsMatch;

        }
        
        private void importExcelDataToLists()
        {
            FileInfo workshopFile = new FileInfo(workshopInfoFilePath);
            using (ExcelPackage currentExcelFile = new ExcelPackage(workshopFile))
            {
                ExcelWorkbook currentWorkbook = currentExcelFile.Workbook;
                if (currentWorkbook != null)
                {

                    if (!checkSheetsMatch(currentWorkbook))
                    {
                        statusText = "Sheets do not have correct names upload a new file";
                    }
                    else
                    {
                        ExcelWorksheet roomSheet = currentWorkbook.Worksheets["Rooms"];
                        ExcelWorksheet presentersSheet = currentWorkbook.Worksheets["Presenters"];
                        
                        createRoomList(roomSheet);
                        createPresenterList(presentersSheet);
                        createSessionList();

                    }
                }
            }
            statusText += "\nWorkshop data extracted:\n     room, presenter, and session lists created.\n";
            

            FileInfo requestFile = new FileInfo(requestFilePath);
            using (ExcelPackage currentExcelFile = new ExcelPackage(requestFile))
            {
                ExcelWorkbook currentWorkbook = currentExcelFile.Workbook;
                if (currentWorkbook != null)
                {
                    ExcelWorksheet requestSheet = currentWorkbook.Worksheets[1];
                    createStudentAndRequestList(requestSheet);
                }

            }
            statusText += "Student requests extracted:\n     student and request lists generated";
        
        }

        #region Create Lists

        /// <summary>
        /// Creates the room list
        /// </summary>
        /// <param name="roomSheet">Extracted Excel 2007+ data</param>
        private void createRoomList(ExcelWorksheet roomSheet)
        {
            roomList = new List<Room>();
            String roomName,errorRoom;
            int roomCapacity;
            errorRoom = "";

            try
            {
                for (int row = 2; row <= roomSheet.Dimension.End.Row; row++)
                {
                    roomName = (String)roomSheet.Cells[row, 1].Value.ToString();
                    roomCapacity = Convert.ToInt32(roomSheet.Cells[row, 2].Value);
                    roomList.Add(new Room(roomName, roomCapacity));
                    errorRoom = roomName;
                }
            }
            catch (Exception currentException)
            {
                MessageBox.Show("last successful room was: " + errorRoom + " check the .XLSX file");
                Application.Exit();
            }


            

        }

        /// <summary>
        /// Creates the presenter list.  Must be completed after the room list as it uses that for a search for specific room object
        /// </summary>
        /// <param name="presentersSheet"></param>
        private void createPresenterList(ExcelWorksheet presentersSheet)
        {
            presenterList = new List<Presenter>();

            String PresenterTitle, presenterRoom, errorPresenter;
            errorPresenter = "";
            try
            {
                for (int row = 2; row <= presentersSheet.Dimension.End.Row; row++)
                {
                    PresenterTitle = (String)presentersSheet.Cells[row, 1].Value;
                    presenterRoom = (String)presentersSheet.Cells[row, 2].Value.ToString();

                    Room currentRoom = roomList.Find(delegate(Room current) { return current.RoomName.Equals(presenterRoom); });
                    presenterList.Add(new Presenter(PresenterTitle, currentRoom));
                    errorPresenter = PresenterTitle;
                }
            }
            catch (Exception currentException)
            {
                MessageBox.Show("last successful presenter was: " + errorPresenter + " check the .XLSX file");
                Application.Exit();
            }
        }

        /// <summary>
        /// Creates the linked student and request sheets
        /// </summary>
        /// <param name="requestSheet">Extracted Excel 2007+ data</param>
        private void createStudentAndRequestList(ExcelWorksheet requestSheet)
        {

            string firstName, lastName, requestOne, requestTwo, requestThree, requestFour, requestFive, studentGradeLevel, studentTeacher, studentNumber, errorStudent;
            int studentID;
            DateTime studentTime;
            errorStudent = "";
            studentList = new List<Student>();
            requestList = new List<Request>();

            try
            {
                for (int row = 2; row <= requestSheet.Dimension.End.Row; row++)
                {
                    studentID = row - 1;

                    double serialDate = double.Parse(requestSheet.Cells[row, 1].Value.ToString());
                    studentTime = DateTime.FromOADate(serialDate);
                    firstName = (String)requestSheet.Cells[row, 2].Value.ToString();
                    lastName = (String)requestSheet.Cells[row, 3].Value.ToString();
                    requestOne = (String)requestSheet.Cells[row, 4].Value.ToString();
                    requestTwo = (String)requestSheet.Cells[row, 5].Value.ToString();
                    requestThree = (String)requestSheet.Cells[row, 6].Value.ToString();
                    requestFour = (String)requestSheet.Cells[row, 7].Value.ToString();
                    requestFive = (String)requestSheet.Cells[row, 8].Value.ToString();
                    studentGradeLevel = (String)requestSheet.Cells[row, 9].Value.ToString();
                    studentNumber = (String)requestSheet.Cells[row, 10].Value.ToString();
                    studentTeacher = (String)requestSheet.Cells[row, 11].Value.ToString();

                    errorStudent = lastName + ", " + firstName;

                    Request currentRequest;
                    Student currentStudent = new Student(firstName, lastName, studentNumber, studentTeacher,  studentGradeLevel);
                    currentRequest = new Request(currentStudent, studentTime, requestOne, requestTwo, requestThree, requestFour, requestFive);
                    currentStudent.StudentRequest = currentRequest;

                    requestList.Add(currentRequest);
                    studentList.Add(currentStudent);

                }

            }
            catch (Exception currentException)
            {
                MessageBox.Show("last successful student was: " + errorStudent + " check the .XLSX file");
                Application.Exit();
            }
        }

        /// <summary>
        /// Creates the session list.  Must occur after the presenter list is created.
        /// </summary>
        private void createSessionList()
        {
            sessionList = new List<Session>();
            foreach (Presenter currentPresenter in presenterList)
            {
                sessionList.Add(new Session(currentPresenter));
            }

        }

        #endregion


        private void gradeBasedSchedules()
        {
            List<Student> sixthStudents = studentList.FindAll(delegate(Student tempSixthStudent) { return (tempSixthStudent.GradeLevel.Substring(0, 1).Equals("6") && ((tempSixthStudent.StudentRequest.RequestOne.Substring(0, 1).Equals("6")) || (tempSixthStudent.StudentRequest.RequestTwo.Substring(0, 1).Equals("6")) || (tempSixthStudent.StudentRequest.RequestThree.Substring(0, 1).Equals("6")))); });
            List<Student> seventhStudents = studentList.FindAll(delegate(Student tempSeventhStudent) { return (tempSeventhStudent.GradeLevel.Substring(0, 1).Equals("7") && ((tempSeventhStudent.StudentRequest.RequestOne.Substring(0, 1).Equals("7")) || (tempSeventhStudent.StudentRequest.RequestTwo.Substring(0, 1).Equals("7")) || (tempSeventhStudent.StudentRequest.RequestThree.Substring(0, 1).Equals("7")))); });
            List<Student> eighthStudents = studentList.FindAll(delegate(Student tempEighthStudent) { return (tempEighthStudent.GradeLevel.Substring(0, 1).Equals("8") && ((tempEighthStudent.StudentRequest.RequestOne.Substring(0, 1).Equals("8")) || (tempEighthStudent.StudentRequest.RequestTwo.Substring(0, 1).Equals("8")) || (tempEighthStudent.StudentRequest.RequestThree.Substring(0, 1).Equals("8")))); });
            

        
        
        }

        public void generateTeamSchedules()
        {
            List<Student> teamStudents = studentList.FindAll(delegate(Student tempTeamStudent) { return ((tempTeamStudent.StudentRequest.RequestOne.Substring(0, 4).Equals("team", StringComparison.OrdinalIgnoreCase)) || (tempTeamStudent.StudentRequest.RequestTwo.Substring(0, 4).Equals("team", StringComparison.OrdinalIgnoreCase)) || (tempTeamStudent.StudentRequest.RequestThree.Substring(0, 4).Equals("team", StringComparison.OrdinalIgnoreCase))); });
            //check name size for n/a to avoid
            #region First Day Team requests
            foreach (KeyValuePair<string, int> currentFirstRequest in FirstClasses)
            {
                if(currentFirstRequest.Key.Substring(0,4).Equals("team", StringComparison.OrdinalIgnoreCase))
                {
                    Session currentFirstSession = sessionList.Find(delegate(Session currentFirst) { return currentFirst.SessionPresenter.PresenterTitle.Equals(currentFirstRequest.Key); });

                    Room currentRoom = roomList.Find(delegate (Room firstRoom) { return firstRoom.Equals(currentFirstSession.SessionPresenter.PresenterRoom); });
                    
                    int currentCapacity = currentRoom.RoomCapacity;

                    int currentSessionACount = currentFirstSession.SessionOneCount;
                    
                    List<Request> wantedTeamClasses = requestList.FindAll(delegate (Request firstTeamRequest) { return (firstTeamRequest.RequestOne.Equals(currentFirstSession.SessionPresenter.PresenterTitle));});

                    foreach (Request currentFirstTeamRequest in wantedTeamClasses)
                    {
                        Student currentTeamStudent = studentList.Find(delegate(Student teamStudent) { return teamStudent.Equals(currentFirstTeamRequest.RequestingStudent); });

                        if (currentFirstTeamRequest.RequestOne.Equals(currentFirstRequest.Key))
                        {
                            if ((currentTeamStudent.SessionOne.Length == 0) && (currentSessionACount < currentCapacity))
                            {
                                currentTeamStudent.SessionOne = currentFirstRequest.Key;
                                currentSessionACount++;
                                currentFirstSession.SessionOneList.Add(currentTeamStudent);
                            }
                        }
                    }
                }
                    
            }
            #endregion
                
            #region Second Day Team requests

            foreach (KeyValuePair<string, int> currentSecondRequest in SecondClasses)
            {
                if (currentSecondRequest.Key.Substring(0, 4).Equals("team", StringComparison.OrdinalIgnoreCase))
                {

                    Session currentSecondSession = sessionList.Find(delegate(Session currentSecond) { return currentSecond.SessionPresenter.PresenterTitle.Equals(currentSecondRequest.Key); });

                    Room currentRoom = roomList.Find(delegate(Room secondRoom) { return secondRoom.Equals(currentSecondSession.SessionPresenter.PresenterRoom); });

                    int currentCapacity = currentRoom.RoomCapacity;

                    int currentSessionBCount = currentSecondSession.SessionTwoCount;

                    List<Request> wantedTeamClasses = requestList.FindAll(delegate(Request secondTeamRequest) { return (secondTeamRequest.RequestTwo.Equals(currentSecondSession.SessionPresenter.PresenterTitle)); });

                    foreach (Request currentSecondTeamRequest in wantedTeamClasses)
                    {
                        Student currentTeamStudent = studentList.Find(delegate(Student teamStudent) { return teamStudent.Equals(currentSecondTeamRequest.RequestingStudent); });

                        if (currentSecondTeamRequest.RequestTwo.Equals(currentSecondRequest.Key))
                        {
                            if ((currentTeamStudent.SessionTwo.Length == 0) && (currentSessionBCount < currentCapacity))
                            {
                                currentTeamStudent.SessionTwo = currentSecondRequest.Key;
                                currentSessionBCount++;
                                currentSecondSession.SessionTwoList.Add(currentTeamStudent);
                            }
                        }
                    }
                }
            }
            #endregion

            #region Third day Team requests
            foreach (KeyValuePair<string, int> currentThirdRequest in ThirdClasses)
            {
                if (currentThirdRequest.Key.Substring(0, 4).Equals("team", StringComparison.OrdinalIgnoreCase))
                {
                    Session currentThirdSession = sessionList.Find(delegate(Session currentThird) { return currentThird.SessionPresenter.PresenterTitle.Equals(currentThirdRequest.Key); });

                    Room currentRoom = roomList.Find(delegate(Room thirdRoom) { return thirdRoom.Equals(currentThirdSession.SessionPresenter.PresenterRoom); });

                    int currentCapacity = currentRoom.RoomCapacity;

                    int currentSessionCCount = currentThirdSession.SessionThreeCount;

                    List<Request> wantedTeamClasses = requestList.FindAll(delegate(Request thirdTeamRequest) { return (thirdTeamRequest.RequestThree.Equals(currentThirdSession.SessionPresenter.PresenterTitle)); });

                    foreach (Request currentThirdTeamRequest in wantedTeamClasses)
                    {
                        Student currentTeamStudent = studentList.Find(delegate(Student teamStudent) { return teamStudent.Equals(currentThirdTeamRequest.RequestingStudent); });

                        if (currentThirdTeamRequest.RequestThree.Equals(currentThirdRequest.Key))
                        {
                            if ((currentTeamStudent.SessionThree.Length == 0) && (currentSessionCCount < currentCapacity))
                            {
                                currentTeamStudent.SessionThree = currentThirdRequest.Key;
                                currentSessionCCount++;
                                currentThirdSession.SessionThreeList.Add(currentTeamStudent);
                            }
                        }
                    }
                }
            }
            #endregion

            #region Schedule Team students for their remainder of the week
            teamStudents = studentList.FindAll(delegate(Student tempTeamStudent) { return (((tempTeamStudent.StudentRequest.RequestOne.Length > 4 && tempTeamStudent.StudentRequest.RequestOne.Substring(0, 4).Equals("team", StringComparison.OrdinalIgnoreCase)) || (tempTeamStudent.StudentRequest.RequestTwo.Length > 4 && tempTeamStudent.StudentRequest.RequestTwo.Substring(0, 4).Equals("team", StringComparison.OrdinalIgnoreCase)) || (tempTeamStudent.StudentRequest.RequestThree.Length > 4 && tempTeamStudent.StudentRequest.RequestThree.Substring(0, 4).Equals("team", StringComparison.OrdinalIgnoreCase)))); });

            foreach (Student currentTeamStudent in teamStudents)
            {
                if (currentTeamStudent.SessionOne.Length == 0)
                {
                    Session currentFirstSession = sessionList.Find(delegate(Session currentSession) { return (currentSession.SessionPresenter.PresenterTitle.Equals(currentTeamStudent.StudentRequest.RequestOne)); });
                    if (currentFirstSession.SessionOneCount < currentFirstSession.SessionPresenter.PresenterRoom.RoomCapacity)
                    {
                        currentTeamStudent.SessionOne = currentFirstSession.SessionPresenter.PresenterTitle;
                        currentFirstSession.SessionOneList.Add(currentTeamStudent);
                    }
                }
                if (currentTeamStudent.SessionTwo.Length == 0)
                {
                    Session currentSecondSession = sessionList.Find(delegate(Session currentSession) { return (currentSession.SessionPresenter.PresenterTitle.Equals(currentTeamStudent.StudentRequest.RequestTwo)); });
                    if (currentSecondSession.SessionTwoCount < currentSecondSession.SessionPresenter.PresenterRoom.RoomCapacity)
                    {
                        currentTeamStudent.SessionTwo = currentSecondSession.SessionPresenter.PresenterTitle;
                        currentSecondSession.SessionTwoList.Add(currentTeamStudent);
                    }
                }
                if (currentTeamStudent.SessionThree.Length == 0)
                {
                    Session currentThirdSession = sessionList.Find(delegate(Session currentSession) { return (currentSession.SessionPresenter.PresenterTitle.Equals(currentTeamStudent.StudentRequest.RequestThree)); });
                    if (currentThirdSession.SessionThreeCount < currentThirdSession.SessionPresenter.PresenterRoom.RoomCapacity)
                    {
                        currentTeamStudent.SessionThree = currentThirdSession.SessionPresenter.PresenterTitle;
                        currentThirdSession.SessionThreeList.Add(currentTeamStudent);
                    }
                }
            }
            #endregion
            List<Student> blankTeam = teamStudents.FindAll(delegate(Student tempTeamStudent) { return (tempTeamStudent.SessionOne.Length == 0 || tempTeamStudent.SessionTwo.Length == 0 || tempTeamStudent.SessionThree.Length == 0); });
        }

        public void generateScheduleFromLists()
        {
            List<Student> blankStudents = studentList.FindAll(delegate(Student tempStudent) { return (tempStudent.SessionOne.Length == 0 || tempStudent.SessionTwo.Length == 0 || tempStudent.SessionThree.Length == 0 || tempStudent.SessionOne.Equals(exceedsCount) || tempStudent.SessionTwo.Equals(exceedsCount) || tempStudent.SessionThree.Equals(exceedsCount)); });
            
            districtClassCount = new List<DistrictClass>();

            
            #region Basic scheduling
            foreach (KeyValuePair<string, int> classNameAndCount in AllClasses)
            {
                Session currentSession = sessionList.Find(delegate(Session current) { return current.SessionPresenter.PresenterTitle.Equals(classNameAndCount.Key); });
                
                Room currentRoom = roomList.Find(delegate(Room tempRoom) { return tempRoom.Equals(currentSession.SessionPresenter.PresenterRoom); });
                
                int currentCapacity = currentRoom.RoomCapacity;

                int currentSessionACount = currentSession.SessionOneCount;
                int currentSessionBCount = currentSession.SessionTwoCount;
                int currentSessionCCount = currentSession.SessionThreeCount;

                List<Request> wantedClasses = requestList.FindAll(delegate(Request tempRequest) { return ( tempRequest.RequestThree.Equals(currentSession.SessionPresenter.PresenterTitle) || tempRequest.RequestTwo.Equals(currentSession.SessionPresenter.PresenterTitle) || tempRequest.RequestOne.Equals(currentSession.SessionPresenter.PresenterTitle)); });

                
                foreach (Request currentRequest in wantedClasses)
                {

                    Student currentStudent = studentList.Find(delegate(Student temp) { return temp.Equals(currentRequest.RequestingStudent); });

                    #region Student Based Viking Time selection

                    if(! (currentRequest.RequestOne.Substring(0,1).Equals("6") || currentRequest.RequestOne.Substring(0,1).Equals("7") || currentRequest.RequestOne.Substring(0,1).Equals("8") ))
                    {


                        if (currentRequest.RequestOne.Equals(classNameAndCount.Key))
                        {

                            if ((currentStudent.SessionOne.Length == 0) && (currentSessionACount < currentCapacity))
                            {
                                currentStudent.SessionOne = classNameAndCount.Key;
                                currentSessionACount++;
                                currentSession.SessionOneList.Add(currentStudent);
                            }

                        }
                        if (currentRequest.RequestTwo.Equals(classNameAndCount.Key))
                        {

                            if ((currentStudent.SessionTwo.Length == 0) && (currentSessionBCount < currentCapacity))
                            {
                                currentStudent.SessionTwo = classNameAndCount.Key;
                                currentSessionBCount++;
                                currentSession.SessionTwoList.Add(currentStudent);
                            }

                        }
                        if (currentRequest.RequestThree.Equals(classNameAndCount.Key))
                        {

                            if ((currentStudent.SessionThree.Length == 0) && (currentSessionCCount < currentCapacity))
                            {
                                currentStudent.SessionThree = classNameAndCount.Key;
                                currentSessionCCount++;
                                currentSession.SessionThreeList.Add(currentStudent);
                            }

                        }
                    }
                    else
                    {
                        if(currentStudent.GradeLevel.Substring(0,1).Equals(currentRequest.RequestOne.Substring(0,1)))
                        {
                            if (currentRequest.RequestOne.Equals(classNameAndCount.Key))
                            {

                                if ((currentStudent.SessionOne.Length == 0) && (currentSessionACount < currentCapacity))
                                {
                                    currentStudent.SessionOne = classNameAndCount.Key;
                                    currentSessionACount++;
                                    currentSession.SessionOneList.Add(currentStudent);
                                }

                            }
                        }

                        if(currentStudent.GradeLevel.Substring(0,1).Equals(currentRequest.RequestTwo.Substring(0,1)))
                        {
                             if (currentRequest.RequestTwo.Equals(classNameAndCount.Key))
                            {

                                if ((currentStudent.SessionTwo.Length == 0) && (currentSessionBCount < currentCapacity))
                                {
                                    currentStudent.SessionTwo = classNameAndCount.Key;
                                    currentSessionBCount++;
                                    currentSession.SessionTwoList.Add(currentStudent);
                                }

                            }
                        }

                        if(currentStudent.GradeLevel.Substring(0,1).Equals(currentRequest.RequestThree.Substring(0,1)))
                        {
                            if (currentRequest.RequestThree.Equals(classNameAndCount.Key))
                            {

                                if ((currentStudent.SessionThree.Length == 0) && (currentSessionCCount < currentCapacity))
                                {
                                    currentStudent.SessionThree = classNameAndCount.Key;
                                    currentSessionCCount++;
                                    currentSession.SessionThreeList.Add(currentStudent);
                                }

                            }
                        }
                    }

                    #endregion
              
                }

                
            }
            #endregion
            
            blankStudents = studentList.FindAll(delegate(Student tempStudent) { return (tempStudent.SessionOne.Length == 0 || tempStudent.SessionTwo.Length == 0 || tempStudent.SessionThree.Length == 0); });
            
            extraScheduling();
            
            blankStudents = studentList.FindAll(delegate(Student tempStudent) { return (tempStudent.SessionOne.Length == 0 || tempStudent.SessionTwo.Length == 0 || tempStudent.SessionThree.Length == 0); });
            
            statusText = exceedsMessage + "\n"+blankStudents.Count + " students were not scheduled";
           
        }

        private void extraScheduling()
        {
            #region Somehow scheduling missed this student

            #region Backup first day schedule.

            //AllClasses = AllClasses.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            List<Student> emptyOneStudents = studentList.FindAll(delegate(Student tempStudent) { return (tempStudent.SessionOne.Length == 0 ); });
            foreach (Student missingFirst in emptyOneStudents)
            {
                Request currentExtraRequest = requestList.Find(delegate(Request backup) { return (backup.RequestingStudent.Equals(missingFirst)); });
                Session extraFourthSession = sessionList.Find(delegate(Session backup) { return (backup.SessionPresenter.PresenterTitle.Equals(currentExtraRequest.RequestFour)); });
                Session extraFifthSession = sessionList.Find(delegate(Session backup) { return (backup.SessionPresenter.PresenterTitle.Equals(currentExtraRequest.RequestFive)); });

                if (extraFourthSession != null && (extraFourthSession.SessionOneCount < extraFourthSession.SessionPresenter.PresenterRoom.RoomCapacity))
                {
                    if (missingFirst.SessionOne.Length == 0)
                    {
                        if(! (extraFourthSession.SessionPresenter.PresenterTitle.Substring(0,1).Equals("6") || extraFourthSession.SessionPresenter.PresenterTitle.Substring(0,1).Equals("7") || extraFourthSession.SessionPresenter.PresenterTitle.Substring(0,1).Equals("8")) )
                        {
                            missingFirst.SessionOne = extraFourthSession.SessionPresenter.PresenterTitle;
                            extraFourthSession.SessionOneList.Add(missingFirst);
                        }
                    }
                }
                if (extraFifthSession != null && (extraFifthSession.SessionOneCount < extraFifthSession.SessionPresenter.PresenterRoom.RoomCapacity))
                {
                    if (missingFirst.SessionOne.Length == 0)
                    {
                        if(! (extraFifthSession.SessionPresenter.PresenterTitle.Substring(0,1).Equals("6") || extraFifthSession.SessionPresenter.PresenterTitle.Substring(0,1).Equals("7") || extraFifthSession.SessionPresenter.PresenterTitle.Substring(0,1).Equals("8") ))
                        {
                            missingFirst.SessionOne = extraFifthSession.SessionPresenter.PresenterTitle;
                            extraFifthSession.SessionOneList.Add(missingFirst);
                        }
                    }
                }
                if(missingFirst.SessionOne.Length==0)
                {
                    stillOneBlank = sessionList.FindAll(delegate(Session tempSession) { return (tempSession.SessionOneCount < tempSession.SessionPresenter.PresenterRoom.RoomCapacity); });
                    foreach (Session emptyOne in stillOneBlank)
                    {
                        if (missingFirst.SessionOne.Length == 0 && emptyOne.SessionOneCount < emptyOne.Capacity)
                        {
                            if(! (emptyOne.SessionPresenter.PresenterTitle.Substring(0,1).Equals("6") || emptyOne.SessionPresenter.PresenterTitle.Substring(0,1).Equals("7") || emptyOne.SessionPresenter.PresenterTitle.Substring(0,1).Equals("8") || emptyOne.SessionPresenter.PresenterTitle.Substring(0,4).Equals("team", StringComparison.OrdinalIgnoreCase) ))
                            {
                                missingFirst.SessionOne = emptyOne.SessionPresenter.PresenterTitle;
                                emptyOne.SessionOneList.Add(missingFirst);
                            }
                            else
                            {
                                if(missingFirst.GradeLevel.Substring(0,1).Equals(emptyOne.SessionPresenter.PresenterTitle.Substring(0,1)))
                                {
                                    missingFirst.SessionOne = emptyOne.SessionPresenter.PresenterTitle;
                                    emptyOne.SessionOneList.Add(missingFirst);
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            emptyOneStudents = studentList.FindAll(delegate(Student tempStudent) { return (tempStudent.SessionOne.Length == 0); });
            
            int testOne;
            #region Backup second day schedule.
            List<Student> emptyTwoStudents = studentList.FindAll(delegate(Student tempStudent) { return (tempStudent.SessionTwo.Length == 0); });
            foreach (Student missingSecond in emptyTwoStudents)
            {
                Request currentExtraRequest = requestList.Find(delegate(Request backup) { return (backup.RequestingStudent.Equals(missingSecond)); });
                Session extraFourthSession = sessionList.Find(delegate(Session backup) { return (backup.SessionPresenter.PresenterTitle.Equals(currentExtraRequest.RequestFour)); });
                Session extraFifthSession = sessionList.Find(delegate(Session backup) { return (backup.SessionPresenter.PresenterTitle.Equals(currentExtraRequest.RequestFive)); });

                if (extraFourthSession != null && (extraFourthSession.SessionTwoCount < extraFourthSession.SessionPresenter.PresenterRoom.RoomCapacity))
                {
                    if (missingSecond.SessionTwo.Length == 0)
                    {
                        if(! (extraFourthSession.SessionPresenter.PresenterTitle.Substring(0,1).Equals("6") || extraFourthSession.SessionPresenter.PresenterTitle.Substring(0,1).Equals("7") || extraFourthSession.SessionPresenter.PresenterTitle.Substring(0,1).Equals("8")) )
                        {
                            missingSecond.SessionTwo = extraFourthSession.SessionPresenter.PresenterTitle;
                            extraFourthSession.SessionTwoList.Add(missingSecond);
                        }
                    }
                }
                if (extraFifthSession != null && (extraFifthSession.SessionTwoCount < extraFifthSession.SessionPresenter.PresenterRoom.RoomCapacity))
                {
                    if (missingSecond.SessionTwo.Length == 0)
                    {
                        if(! (extraFifthSession.SessionPresenter.PresenterTitle.Substring(0,1).Equals("6") || extraFifthSession.SessionPresenter.PresenterTitle.Substring(0,1).Equals("7") || extraFifthSession.SessionPresenter.PresenterTitle.Substring(0,1).Equals("8") ))
                        {
                            missingSecond.SessionTwo = extraFifthSession.SessionPresenter.PresenterTitle;
                            extraFifthSession.SessionTwoList.Add(missingSecond);
                        }
                    }
                }
                if (missingSecond.SessionTwo.Length == 0)
                {
                    stillTwoBlank = sessionList.FindAll(delegate(Session tempSession) { return (tempSession.SessionTwoCount < tempSession.SessionPresenter.PresenterRoom.RoomCapacity); });
                    foreach (Session emptyTwo in stillTwoBlank)
                    {
                        if (missingSecond.SessionTwo.Length == 0 && emptyTwo.SessionTwoCount < emptyTwo.Capacity)
                        {

                            if (!(emptyTwo.SessionPresenter.PresenterTitle.Substring(0, 1).Equals("6") || emptyTwo.SessionPresenter.PresenterTitle.Substring(0, 1).Equals("7") || emptyTwo.SessionPresenter.PresenterTitle.Substring(0, 1).Equals("8") || emptyTwo.SessionPresenter.PresenterTitle.Substring(0, 4).Equals("team", StringComparison.OrdinalIgnoreCase)))
                            {
                                missingSecond.SessionTwo = emptyTwo.SessionPresenter.PresenterTitle;
                                emptyTwo.SessionTwoList.Add(missingSecond);
                            }
                            else
                            {
                                if (missingSecond.GradeLevel.Substring(0, 1).Equals(emptyTwo.SessionPresenter.PresenterTitle.Substring(0, 1)))
                                {
                                    missingSecond.SessionTwo = emptyTwo.SessionPresenter.PresenterTitle;
                                    emptyTwo.SessionTwoList.Add(missingSecond);
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            emptyTwoStudents = studentList.FindAll(delegate(Student tempStudent) { return (tempStudent.SessionTwo.Length == 0); });
            int testTwo;
            #region Backup third day schedule
            List<Student> emptyThreeStudents = studentList.FindAll(delegate(Student tempStudent) { return (tempStudent.SessionThree.Length == 0); });

            foreach (Student missingThird in emptyThreeStudents)
            {
                Request currentExtraRequest = requestList.Find(delegate(Request backup) { return (backup.RequestingStudent.Equals(missingThird)); });
                Session extraFourthSession = sessionList.Find(delegate(Session backup) { return (backup.SessionPresenter.PresenterTitle.Equals(currentExtraRequest.RequestFour)); });
                Session extraFifthSession = sessionList.Find(delegate(Session backup) { return (backup.SessionPresenter.PresenterTitle.Equals(currentExtraRequest.RequestFive)); });

                if (extraFourthSession != null && (extraFourthSession.SessionThreeCount < extraFourthSession.SessionPresenter.PresenterRoom.RoomCapacity))
                {
                    if (missingThird.SessionThree.Length == 0)
                    {
                        if (!(extraFourthSession.SessionPresenter.PresenterTitle.Substring(0, 1).Equals("6") || extraFourthSession.SessionPresenter.PresenterTitle.Substring(0, 1).Equals("7") || extraFourthSession.SessionPresenter.PresenterTitle.Substring(0, 1).Equals("8")))
                        {
                            missingThird.SessionThree = extraFourthSession.SessionPresenter.PresenterTitle;
                            extraFourthSession.SessionThreeList.Add(missingThird);
                        }
                    }
                }
                if (extraFifthSession != null && (extraFifthSession.SessionThreeCount < extraFifthSession.SessionPresenter.PresenterRoom.RoomCapacity))
                {
                    if (missingThird.SessionThree.Length == 0)
                    {
                        if (!(extraFifthSession.SessionPresenter.PresenterTitle.Substring(0, 1).Equals("6") || extraFifthSession.SessionPresenter.PresenterTitle.Substring(0, 1).Equals("7") || extraFifthSession.SessionPresenter.PresenterTitle.Substring(0, 1).Equals("8")))
                        {
                            missingThird.SessionThree = extraFifthSession.SessionPresenter.PresenterTitle;
                            extraFifthSession.SessionThreeList.Add(missingThird);
                        }
                    }
                }
                if(missingThird.SessionThree.Length==0)
                {
                    stillThreeBlank = sessionList.FindAll(delegate(Session tempSession) { return (tempSession.SessionThreeCount < tempSession.SessionPresenter.PresenterRoom.RoomCapacity); });
                    
                    foreach (Session emptyThree in stillThreeBlank)
                    {
                        if (missingThird.SessionThree.Length == 0 && emptyThree.SessionThreeCount < emptyThree.Capacity)
                        {
                            if (!(emptyThree.SessionPresenter.PresenterTitle.Substring(0, 1).Equals("6") || emptyThree.SessionPresenter.PresenterTitle.Substring(0, 1).Equals("7") || emptyThree.SessionPresenter.PresenterTitle.Substring(0, 1).Equals("8") || emptyThree.SessionPresenter.PresenterTitle.Substring(0, 4).Equals("team", StringComparison.OrdinalIgnoreCase)))
                            {
                                missingThird.SessionThree = emptyThree.SessionPresenter.PresenterTitle;
                                emptyThree.SessionThreeList.Add(missingThird);
                            }
                            else
                            {
                                if (missingThird.GradeLevel.Substring(0, 1).Equals(emptyThree.SessionPresenter.PresenterTitle.Substring(0, 1)))
                                {
                                    missingThird.SessionThree = emptyThree.SessionPresenter.PresenterTitle;
                                    emptyThree.SessionThreeList.Add(missingThird);
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            emptyThreeStudents = studentList.FindAll(delegate(Student tempStudent) { return (tempStudent.SessionThree.Length == 0); });
            int testThree;

            #region Just in case scheduling has completely failed
            List<Student> emptyStudents = studentList.FindAll(delegate(Student tempStudent) { return (tempStudent.SessionOne.Length == 0 || tempStudent.SessionTwo.Length == 0 || tempStudent.SessionThree.Length == 0); });

            foreach (Student currentStudent in emptyStudents)
            {
                stillOneBlank = sessionList.FindAll(delegate(Session tempSession) { return (tempSession.SessionOneCount < tempSession.SessionPresenter.PresenterRoom.RoomCapacity && !tempSession.SessionPresenter.PresenterTitle.Substring(0,4).Equals("team",  StringComparison.OrdinalIgnoreCase)); });
                stillTwoBlank = sessionList.FindAll(delegate(Session tempSession) { return (tempSession.SessionTwoCount < tempSession.SessionPresenter.PresenterRoom.RoomCapacity && !tempSession.SessionPresenter.PresenterTitle.Substring(0, 4).Equals("team", StringComparison.OrdinalIgnoreCase)); });
                stillThreeBlank = sessionList.FindAll(delegate(Session tempSession) { return (tempSession.SessionThreeCount < tempSession.SessionPresenter.PresenterRoom.RoomCapacity && !tempSession.SessionPresenter.PresenterTitle.Substring(0, 4).Equals("team", StringComparison.OrdinalIgnoreCase)); });

                stillOneBlank = stillOneBlank.OrderBy(x => x.SessionOneCount).ToList<Session>();
                stillTwoBlank = stillTwoBlank.OrderBy(x => x.SessionTwoCount).ToList<Session>();
                stillThreeBlank = stillThreeBlank.OrderBy(x => x.SessionThreeCount).ToList<Session>();
                
                if (stillOneBlank.Count > 0 && currentStudent.SessionOne.Length == 0)
                {
                    foreach (Session currentSession in stillOneBlank)
                    {
                        if (currentStudent.SessionOne.Length == 0)
                        {
                            int currentCount = currentSession.SessionOneCount;
                            int currentCap = currentSession.Capacity;
                            
                            if (currentCount < currentCap )
                            {
                                currentStudent.SessionOne = currentSession.SessionPresenter.PresenterTitle;
                                currentSession.SessionOneList.Add(currentStudent);
                            }
                        }
                    }
                }

                if (stillTwoBlank.Count > 0 && currentStudent.SessionTwo.Length == 0)
                {
                    foreach (Session currentSession in stillTwoBlank)
                    {
                        if (currentStudent.SessionTwo.Length == 0)
                        {
                            int currentCount = currentSession.SessionTwoCount;
                            int currentCap = currentSession.Capacity;
                            
                            if (currentCount < currentCap )
                            {
                                currentStudent.SessionTwo = currentSession.SessionPresenter.PresenterTitle;
                                currentSession.SessionTwoList.Add(currentStudent);
                            }
                        }
                    }
                }
                if (stillThreeBlank.Count > 0 && currentStudent.SessionThree.Length == 0)
                {
                    foreach (Session currentSession in stillThreeBlank)
                    {
                        if (currentStudent.SessionThree.Length == 0)
                        {
                            int currentCount = currentSession.SessionThreeCount;
                            int currentCap = currentSession.Capacity;
                            
                            if (currentCount < currentCap  ) 
                            {
                                currentStudent.SessionThree = currentSession.SessionPresenter.PresenterTitle;
                                currentSession.SessionThreeList.Add(currentStudent);
                            }
                        }
                    }
                }
            }
            #endregion
            emptyStudents = studentList.FindAll(delegate(Student tempStudent) { return (tempStudent.SessionOne.Length == 0 || tempStudent.SessionTwo.Length == 0 || tempStudent.SessionThree.Length == 0); });
            
            #endregion

            stupidList = districtClassCount.FindAll(delegate(DistrictClass tempClassCount) { return (tempClassCount.Count < tempClassCount.Max); });

        }


        public void createWorkshopExportExcel()
        {
            int columnCount = 15;
            FileInfo currentFile = new FileInfo(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "\\Viking Time Schedule.xlsx");
            if (currentFile.Exists)
            {
                currentFile.Delete();  // ensures we create a new workbook
                currentFile = new FileInfo(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "\\Viking Time Schedule.xlsx");
            }

            using (ExcelPackage currentExcel = new ExcelPackage(currentFile))
            {
                ExcelWorksheet currentSheet = currentExcel.Workbook.Worksheets.Add("Student Registrations");
                Presenter workshopPresenter;
                int currentRowCounter = 2;

                currentSheet.Cells["A1"].Value = "Student First Name";
                currentSheet.Cells["B1"].Value = "Student Last Name";
                currentSheet.Cells["C1"].Value = "Student Grade Level";
                currentSheet.Cells["D1"].Value = "Team Name";
                currentSheet.Cells["E1"].Value = "Home Room Teacher";
                currentSheet.Cells["F1"].Value = "Session One Title";
                currentSheet.Cells["G1"].Value = "Session One Room";
                currentSheet.Cells["H1"].Value = "Session Two Title";
                currentSheet.Cells["I1"].Value = "Session Two Room";
                currentSheet.Cells["J1"].Value = "Session Three Title";
                currentSheet.Cells["K1"].Value = "Session Three Room";
                
                currentSheet.Cells["A1"].AutoFitColumns();
                

                String headerRange = "A1:" + Convert.ToChar('A' + columnCount - 1) + 1;

                using (ExcelRange currentRange = currentSheet.Cells[headerRange])
                {
                    currentRange.Style.WrapText = false;
                    currentRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    currentRange.Style.Font.Bold = true;
                    currentRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    currentRange.Style.Fill.BackgroundColor.SetColor(Color.Gray);
                    currentRange.Style.Font.Color.SetColor(Color.White);

                }

                ExcelWorksheet sessionCountSheet = currentExcel.Workbook.Worksheets.Add("Session Counts");
                sessionCountSheet.Cells[1, 1].Value = "Title";
                sessionCountSheet.Cells[1, 2].Value = "Session A Count";
                sessionCountSheet.Cells[1, 3].Value = "Session A Left";
                sessionCountSheet.Cells[1, 4].Value = "Session B Count";
                sessionCountSheet.Cells[1, 5].Value = "Session B Left";
                sessionCountSheet.Cells[1, 6].Value = "Session C Count";
                sessionCountSheet.Cells[1, 7].Value = "Session C Left";

                for (int rowCount = 1; rowCount <= sessionList.Count; rowCount++ )
                {
                    sessionCountSheet.Cells[rowCount+1, 1].Value = sessionList[rowCount - 1].SessionPresenter.PresenterTitle;
                    sessionCountSheet.Cells[rowCount+1, 2].Value = sessionList[rowCount - 1].SessionOneCount;
                    sessionCountSheet.Cells[rowCount + 1, 3].Value = sessionList[rowCount - 1].Capacity - sessionList[rowCount - 1].SessionOneCount;
                    sessionCountSheet.Cells[rowCount+1, 4].Value = sessionList[rowCount - 1].SessionTwoCount;
                    sessionCountSheet.Cells[rowCount + 1, 5].Value = sessionList[rowCount - 1].Capacity - sessionList[rowCount - 1].SessionTwoCount;
                    sessionCountSheet.Cells[rowCount+1, 6].Value = sessionList[rowCount - 1].SessionThreeCount;
                    sessionCountSheet.Cells[rowCount + 1, 7].Value = sessionList[rowCount - 1].Capacity - sessionList[rowCount - 1].SessionThreeCount;
                }
                
                #region Create master schedule sheet
                foreach (Student workshopStudent in studentList)
                {
                    workshopPresenter = presenterList.Find(delegate(Presenter curr) { return curr.PresenterTitle.Equals(workshopStudent.SessionOne); });

                    currentSheet.Cells[currentRowCounter, 1].Value = workshopStudent.FirstName;
                    currentSheet.Cells[currentRowCounter, 2].Value = workshopStudent.LastName;
                    currentSheet.Cells[currentRowCounter, 3].Value = workshopStudent.GradeLevel;
                    currentSheet.Cells[currentRowCounter, 4].Value = workshopStudent.HomeRoomTeacher;
                    currentSheet.Cells[currentRowCounter, 5].Value = workshopStudent.TeamName;

                    if (workshopStudent.SessionOne.Equals(failedCutoff) || workshopStudent.SessionOne.Equals(exceedsCount))
                    {
                        currentSheet.Cells[currentRowCounter, 6].Value = workshopStudent.SessionOne;
                        currentSheet.Cells[currentRowCounter, 7].Value = workshopStudent.SessionOne;
                    }
                    else
                    {
                        currentSheet.Cells[currentRowCounter, 6].Value = workshopStudent.SessionOne;
                        workshopPresenter = presenterList.Find(delegate(Presenter curr) { return curr.PresenterTitle.Equals(workshopStudent.SessionOne); });
                        currentSheet.Cells[currentRowCounter, 7].Value = workshopPresenter.PresenterRoom.RoomName;
                    }
                    if (workshopStudent.SessionTwo.Equals(failedCutoff) || workshopStudent.SessionTwo.Equals(exceedsCount))
                    {
                        currentSheet.Cells[currentRowCounter, 8].Value = workshopStudent.SessionTwo;
                        currentSheet.Cells[currentRowCounter, 9].Value = workshopStudent.SessionTwo;
                    }
                    else
                    {
                        currentSheet.Cells[currentRowCounter, 8].Value = workshopStudent.SessionTwo;
                        workshopPresenter = presenterList.Find(delegate(Presenter curr) { return curr.PresenterTitle.Equals(workshopStudent.SessionTwo); });
                        currentSheet.Cells[currentRowCounter, 9].Value = workshopPresenter.PresenterRoom.RoomName;
                    }
                    if (workshopStudent.SessionThree.Equals(failedCutoff) || workshopStudent.SessionThree.Equals(exceedsCount))
                    {
                        currentSheet.Cells[currentRowCounter, 10].Value = workshopStudent.SessionThree;
                        currentSheet.Cells[currentRowCounter, 11].Value = workshopStudent.SessionThree;
                    }
                    else
                    {
                        currentSheet.Cells[currentRowCounter, 10].Value = workshopStudent.SessionThree;
                        workshopPresenter = presenterList.Find(delegate(Presenter curr) { return curr.PresenterTitle.Equals(workshopStudent.SessionThree); });
                        currentSheet.Cells[currentRowCounter, 11].Value = workshopPresenter.PresenterRoom.RoomName;
                    }

                    currentRowCounter++;

                }
                #endregion

                String rowsCellRange = "A2:" + Convert.ToChar('A' + columnCount - 1) + (studentList.Count + 1);
                using (ExcelRange currentRange = currentSheet.Cells[rowsCellRange])
                {
                    currentRange.Style.WrapText = true;
                    currentRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    
                }
                
                String currentPresenterTitle;
                int currentRowCounterA = 2, currentRowCounterB = 2, currentRowCounterC = 2;
                int roomNumber = 1;
                #region Create presenter rolls with attending student names
                foreach (Presenter currentPresenter in presenterList)
                {
                    Session currentSession = sessionList.Find(delegate(Session curr) { return (curr.SessionPresenter.PresenterTitle.Equals(currentPresenter.PresenterTitle)); });

                    if (currentPresenter.PresenterTitle.Length > 28)
                    {
                        currentPresenterTitle = currentPresenter.PresenterTitle.Substring(0, 23) + roomNumber;
                        roomNumber++;
                    }
                    else
                    {
                        currentPresenterTitle = currentPresenter.PresenterTitle;
                    }

                    ExcelWorksheet currentSheetA = currentExcel.Workbook.Worksheets.Add(currentPresenterTitle + " A");
                    ExcelWorksheet currentSheetB = currentExcel.Workbook.Worksheets.Add(currentPresenterTitle + " B");
                    ExcelWorksheet currentSheetC = currentExcel.Workbook.Worksheets.Add(currentPresenterTitle + " C");

                    currentPresenterTitle = currentPresenter.PresenterTitle;

                    currentSheetA.Cells["A1"].Value = currentPresenterTitle;
                    currentSheetA.Cells["B1"].Value = "Session A";
                    currentSheetA.Cells["C1"].Value = "Session Count: " + currentSession.SessionOneCount;

                    currentSheetB.Cells["A1"].Value = currentPresenterTitle;
                    currentSheetB.Cells["B1"].Value = "Session B";
                    currentSheetB.Cells["C1"].Value = "Session Count: " + currentSession.SessionTwoCount;

                    currentSheetC.Cells["A1"].Value = currentPresenterTitle;
                    currentSheetC.Cells["B1"].Value = "Session C";
                    currentSheetC.Cells["C1"].Value = "Session Count: " + currentSession.SessionThreeCount;



                    foreach (Student currentStudent in studentList)
                    {
                       
                        if (currentStudent.SessionOne.Equals(currentPresenterTitle))
                        {
                            //write student name to currentsheetA
                            currentSheetA.Cells[currentRowCounterA, 1].Value = currentStudent.FirstName;
                            currentSheetA.Cells[currentRowCounterA, 2].Value = currentStudent.LastName;
                            currentRowCounterA++;
                        }
                        if (currentStudent.SessionTwo.Equals(currentPresenterTitle))
                        {
                            //write student name to currentsheetB
                            currentSheetB.Cells[currentRowCounterB, 1].Value = currentStudent.FirstName;
                            currentSheetB.Cells[currentRowCounterB, 2].Value = currentStudent.LastName;
                            currentRowCounterB++;
                        }
                        if (currentStudent.SessionThree.Equals(currentPresenterTitle))
                        {
                            //write student name to currentsheetC
                            currentSheetC.Cells[currentRowCounterC, 1].Value = currentStudent.FirstName;
                            currentSheetC.Cells[currentRowCounterC, 2].Value = currentStudent.LastName;
                            currentRowCounterC++;
                        }


                    }

                    //Reset row counters
                    currentRowCounterA = 2;
                    currentRowCounterB = 2;
                    currentRowCounterC = 2;
                }
                #endregion
                currentExcel.Save();
            }
            okToSchedule = true;
        }

            //stillOneBlank = sessionList.FindAll(delegate(Session tempSession) { return (tempSession.SessionOneCount < tempSession.SessionPresenter.PresenterRoom.RoomCapacity); });
        
        public void startSchedule()
        {
            importExcelDataToLists();
        }
    }
}
