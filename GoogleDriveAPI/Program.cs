using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DriveQuickstart
{
    class Program
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        //static string[] Scopes = { DriveService.Scope.DriveReadonly }; // this is read only
        static string[] Scopes = { DriveService.Scope.Drive };
        static string ApplicationName = "Trung Test Google API";

        static void Main(string[] args)
        {
            UserCredential credential;
            credential = GetCredentialsInfor(); 

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });


            //>>TO DO: read input variables from SQL table,text,csc, etc.. ?

            var filepath = @"\\Nas\pv\Shared\RDI\Personal Folders\Trung\Current Projects\Google API\Projects\TrungDriveAPI\GoogleDriveAPI\GoogleDriveAPI\input\TestInput.csv";
            readCSVfileInput(service, filepath);

            //CreateFolder(service);
            //string pageToken = null;
            //do {ListOfFiles(service, ref pageToken);} while (pageToken != null);

            /*
            //SearchAndCreateFolder(service,"Reports","trantr");
            //> Temp input
            string rootFolder = "Reports";
            string userIDSubFolder = "trantr";
            //string userIDSubFolder = "chunj";
            //string userIDSubFolder = "raog";
            string userEmail = "trtran@lincolninvestment.com";
            //string userEmail = "mcarter@lincolninvestment.com";
            //string userEmail = "JChun@LincolnInvestment.com";
            //string userEmail = "grao@LincolnInvestment.com";
            string uploadFilePath = @"\\\nas\\ftpusers\\trantr\\myreports\\test.pdf";
            string uploadFileType = "document/pdf";
            //> 

            //> check and receate folder
            SearchAndCreateFolder(service, rootFolder, userIDSubFolder, userEmail);

            //> upload attempt
            bool isUpload = false;
            int maxUploadAttemp = 0;
            do {
                maxUploadAttemp++;
                var rootFolderID = searchAndReturnFolderID(service, userIDSubFolder);  // check for subfolder ID
                if (rootFolderID != null) {
                    //upload
                    var fileID = UploadFile(uploadFilePath, service, rootFolderID, uploadFileType); // network sharedrive
                    //UploadFile("C:\\temp\\SPlogo.png", service);
                    isUpload = true;
                   Console.WriteLine("File Uploaded, ID:" + fileID);
                    //set share permission 
                    ShareFile(service, fileID, userEmail, "writer");
                } else {
                   Thread.Sleep(2000); // wait for another 2 second
                   Console.WriteLine("No Attemp to upload : " + maxUploadAttemp);
                }
            }
            while (isUpload != true || maxUploadAttemp == 100);
            */
            
            Console.WriteLine("Done");
            Console.Read();
            
        }

        //> show list of file
        public static void ListOfFiles(DriveService tempService, ref string pageToken)
        {
          
           // Define parameters of request.
           FilesResource.ListRequest listRequest = tempService.Files.List();
           listRequest.PageSize = 10; // show only 10 item each time
           listRequest.Fields = "nextPageToken, files(id, name)";
           listRequest.PageToken = pageToken;
            //listRequest.Q = "mimeType='image/jpeg'"; // what type of file you want to show
            //"MimeType = 'text/plain'"
            listRequest.Q = "mimeType='application/vnd.google-apps.folder' and trashed=false";

            // List files.
            var request = listRequest.Execute();
            //IList<Google.Apis.Drive.v3.Data.File> request = listRequest.Execute().Files;

            Console.WriteLine("Files:");
           if (request != null && request.Files.Count > 0)
           {
               foreach (var file in request.Files)
               {
                   Console.WriteLine("{0} ({1})", file.Name, file.Id);
                   //Debug.Print("{0} ({1})", file.Name, file.Id);
                }

                pageToken = request.NextPageToken;

                if (request.NextPageToken != null)
                {
                    Console.WriteLine("Press any key to display next Page...");
                    Console.ReadLine();
                }

            }
           else
           {
               Console.WriteLine("No files found.");
           }
           Console.Read();
           
        }

        //> check for root and subfolder before create them if need
        public static void SearchAndCreateFolder(DriveService service, string rootFolder, string subFolder, string userEmail)
        {
            var rootFolderID = searchAndReturnFolderID(service, rootFolder);
            //Console.WriteLine("rooft FolderID: " + rootFolderID);
            if (rootFolderID != null)
            {  // if root folder exist but sub folder is not found then create subfolder 
                Console.WriteLine("rootFolderID : " + rootFolderID);
                // check subfoler > child
                var subFolderID = searchAndReturnSUBFolderID(service, rootFolderID, subFolder);
                //Console.WriteLine("subFolderID : " + subFolderID);
                if (subFolderID == null) // subfolder not found under input name then create new folder
                {
                    var subFolderIDforShare = CreateFolder(service, subFolder, rootFolderID);// service, sub folder, parent ID
                    //set permission 
                    ShareFile(service, subFolderIDforShare, userEmail, "writer");
                } 
            }
            else
            {
                Console.WriteLine("no found rootFolderID");
                // create root folderID found
                CreateFolder(service, rootFolder, null); // service, root folder, parent ID
                // check for root folder ID before call for function to create the sub foler
                bool isfolderFoundID = false;
                int maxWaitTime = 0;
                while (isfolderFoundID == false || maxWaitTime == 150) // 5 minutes max before time out 
                {
                    maxWaitTime++;
                    Console.WriteLine("here: " + maxWaitTime);

                    var tempRootFolderID = searchAndReturnFolderID(service, rootFolder);
                    if (tempRootFolderID != null)
                    {
                        var subFolderIDforShare  = CreateFolder(service, subFolder, tempRootFolderID);// service, sub folder, parent ID
                        isfolderFoundID = true;
                        Console.WriteLine("sub folder created : " + subFolder );
                        //set permission 
                        ShareFile(service, subFolderIDforShare, userEmail, "writer");
                    }
                    else
                    {
                        Console.WriteLine("No Attemp to create subfolder : " + maxWaitTime);
                        Thread.Sleep(2000); // wait for another 2 second
                    }
                } // end while
            }// end of search for root folder id
        }

        //> create a folder either root or subfolder depend on input
        public static string CreateFolder(DriveService tempService, string folderName, string parentID)
        {
            string folderID = null; 

            var fileMetadata = new Google.Apis.Drive.v3.Data.File();
            fileMetadata.Name = folderName;

            if (parentID != null) { // sub folder then add parent ID
                fileMetadata.Parents = new List<string>();
                fileMetadata.Parents.Add(parentID);
            }
            fileMetadata.MimeType = "application/vnd.google-apps.folder";
            var request = tempService.Files.Create(fileMetadata);
            request.Fields = "id,parents";

            try
            {
                var file = request.Execute();
                Console.WriteLine("Folder ID: " + file.Id);
                folderID = file.Id;
                WriteLog("New folder created-> Name:" + folderName + " -> ID: " + file.Id + " -> parents: " + String.Join(", ", file.Parents));
            }
            catch (Exception e)
            {
                WriteLog("An error occurred on new folder create request: " + e.Message);
                Console.WriteLine("An error occurred: " + e.Message);
            }

           

            return folderID;
        }

        // search root foler by name and return the foler ID
        public static string searchAndReturnFolderID(DriveService service, string rootFolderName)
        {
            var searchFilter = "mimeType = 'application/vnd.google-apps.folder' and name = '" + rootFolderName + "' and trashed = false";
            //Debug.Print("searchFilter:  " + searchFilter);

            string folderID = null;
            string pageToken = null;
            do
            {
                var request = service.Files.List();
                request.Q = searchFilter;
                request.Spaces = "drive";
                request.Fields = "nextPageToken, files(id, name)";
                request.PageToken = pageToken;
                var result = request.Execute();
                foreach (var file in result.Files)
                {
                    if (file.Name == rootFolderName) {
                        folderID = file.Id; // found folder and step the loop
                        break;
                    }
                    //Console.WriteLine("{0} ({1})", file.Name, file.Id);
                }
                pageToken = result.NextPageToken;
            } while (pageToken != null);

            return folderID;
        }

        // search by sub folder name and check whether they have a parent or not
        //> children and parents collections have been removed for v2 <<arghhhhhhh use files.list for v3 instead
        public static string searchAndReturnSUBFolderID(DriveService service, string rootFolderID, string subFolderName)
        {
            string subFolderID = null;
            string pageToken = null;
            var searchFilter = "mimeType = 'application/vnd.google-apps.folder' and name = '" + subFolderName + "' and trashed = false";

            do
            {
                var request = service.Files.List();
                request.Q = searchFilter;
                request.Spaces = "drive";
                request.Fields = "nextPageToken, files(id, name, parents)";
                request.PageToken = pageToken;
                var result = request.Execute();
                foreach (var file in result.Files)
                {
                    if (file.Name == subFolderName && file.Parents.Contains(rootFolderID))
                    {
                        subFolderID = file.Id; // found folder and step the loop
                        Console.WriteLine("sub folder found name: "+ file.Name + " >>ID: " + file.Id + " >>Parents: "+ String.Join(",", file.Parents));
                        break;
                    }
                   
                }
                pageToken = result.NextPageToken;
            } while (pageToken != null);

            return subFolderID;
        }
    
        //> Upload file base on input folderID
        //> TO DO:  Check to see if this application has permission to access the input path or not
        public static string UploadFile(string path, DriveService service, string folderId, string uploadType)
        {
            //var folderId = "0BzxTleMimki5dzBoR3p2Z2I3RUk";

            var fileMetadata = new Google.Apis.Drive.v3.Data.File();
            fileMetadata.Name = Path.GetFileName(path);
            //fileMetadata.MimeType = "image/jpeg";
            //fileMetadata.MimeType = "document/pdf";
            fileMetadata.MimeType = uploadType;

            fileMetadata.Parents = new List<string> { folderId }; // location file get upload
            FilesResource.CreateMediaUpload request;
            using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Open))
            {
                //request = service.Files.Create(fileMetadata, stream, "document/pdf");
                request = service.Files.Create(fileMetadata, stream, uploadType);
                request.Fields = "id";
                request.Upload();
            }

            var file = request.ResponseBody;
            WriteLog("New file uploaded -> File ID: " + file.Id + " -> Folder ID: " + folderId + " -> Orginal file Path: " + path);
            Console.WriteLine("File ID: " + file.Id);
            return file.Id;
        }

        //enable collaboeation ability base on input user
        public static void ShareFile(DriveService service, string fileId, string userEmail, string userRole)
        {
            //var fileId = "1sTWaJ_j7PkjzaBWtNc3IzovK5hQf21FbOw9yLeeLPNQ";
            
            var batch = new BatchRequest(service);
            BatchRequest.OnResponse<Permission> callback = delegate (
                Permission permission,
                RequestError error,
                int index,
                System.Net.Http.HttpResponseMessage message)
            {
                if (error != null)
                {
                    // Handle error
                    WriteLog("An error occurred on share permission request: " + error.Message);
                    Console.WriteLine(error.Message);
                }
                else
                {
                    WriteLog("Share Request grant -> User Email:  " + userEmail + " -> userRole: " + userRole + " -> for fileID: " + fileId + " -> with permission.Id: " + permission.Id);
                    Console.WriteLine("Permission ID: " + permission.Id);
                }
            };
            Permission userPermission = new Permission();
            userPermission.Type = "user";
            //userPermission.Role = "writer";
            userPermission.Role = userRole;
            //userPermission.EmailAddress = "mcarter@lincolninvestment.com";
            userPermission.EmailAddress = userEmail;

            var request = service.Permissions.Create(userPermission, fileId);
            request.Fields = "id";
            batch.Queue(request, callback);

            //Permission domainPermission = new Permission();
            //domainPermission.Type = "domain";
            //domainPermission.Role = "reader";
            //domainPermission.Domain = "appsrocks.com";
            //request = service.Permissions.Create(domainPermission, fileId);
            //request.Fields = "id";
            //batch.Queue(request, callback);

            var task = batch.ExecuteAsync();

        }

        //> read input file 
        public static void readCSVfileInput(DriveService service, string path)
        {
            //FileInfo myFile = new FileInfo(@"\\nas\PV\Scratch\temp\SPlogo1.png");
            //bool exists = myFile.Exists;
            //Console.WriteLine("exists: " + exists);

            string filePath = path;
            StreamReader sr = new StreamReader(filePath);
            while (!sr.EndOfStream)
            {
                string[] Line = sr.ReadLine().Split(',');
                //Console.WriteLine("lines[0] " + string.Join(",", Line[0]) + " lines[1] " + string.Join(",", Line[1])  +" lines[2] " + string.Join(",", Line[2]) + " lines[3] " + string.Join(",", Line[3]));

                string rootFolder = "Reports";
                string userIDSubFolder = string.Join(",", Line[0]);
                string userEmail = string.Join(",", Line[1]);
                string uploadFilePath = @"" +string.Join(",", Line[2]);
                string uploadFileType = string.Join(",", Line[3]);
                //> 

                //> check and receate folder
                SearchAndCreateFolder(service, rootFolder, userIDSubFolder, userEmail);

                //> upload attempt
                bool isUpload = false;
                int maxUploadAttemp = 0;
                do
                {
                    maxUploadAttemp++;
                    var rootFolderID = searchAndReturnFolderID(service, userIDSubFolder);  // check for subfolder ID
                    if (rootFolderID != null)
                    {
                        //upload
                        var fileID = UploadFile(uploadFilePath, service, rootFolderID, uploadFileType); // network sharedrive
                                                                                                        //UploadFile("C:\\temp\\SPlogo.png", service);
                        isUpload = true;
                        Console.WriteLine("File Uploaded, ID:" + fileID);
                        //set share permission 
                        ShareFile(service, fileID, userEmail, "writer");
                    }
                    else
                    {
                        Thread.Sleep(2000); // wait for another 2 second
                        Console.WriteLine("No Attemp to upload : " + maxUploadAttemp);
                    }
                }
                while (isUpload != true || maxUploadAttemp == 100);
            }

        }


        //>  write Log to capture files/folder is create/upload ,etc... failed/successful for audit trail 
        public static void WriteLog(string logMessage)
        {
            string logname = String.Format("Log_{0:MM-dd-yyyy}.txt", DateTime.Now);

            using (StreamWriter w = System.IO.File.AppendText(logname))
            {
                Log(logMessage, w);
            }
        }

        public static void DisplayLog()
        {
            string logname = String.Format("Log_{0:MM-dd-yyyy}.txt", DateTime.Now);
            using (StreamReader r = System.IO.File.OpenText(logname)){ DumpLog(r);}
        }

        public static void Log(string logMessage, TextWriter w)
        {
            w.Write("\r\nLog Entry : ");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            w.WriteLine("  :");
            w.WriteLine("  :{0}", logMessage);
            w.WriteLine("-------------------------------");
        }

        public static void DumpLog(StreamReader r)
        {
            string line;
            while ((line = r.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }
        }

        //> Get credentials information
        private static UserCredential GetCredentialsInfor()
        {
            UserCredential credential;

            using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                //Console.WriteLine("Credential file saved to: " + credPath);
            }

            return credential;
        }
    }
}