using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.IO.Compression;
namespace Launcher
{

    internal class LauncherLogic
    {
        const string SERVER_ADRESS = "ftp://eternalrifts.ru/Test_catalog2/";
        const string SERVER_LOGIN = "u1851278_upload";
        const string SERVER_PASSW = "jS9uR8wF8u";
        const string GAME_FOLDER = "Game Data\\";
        private string curFolder = "";
        const int MIN_PACKAGE_SIZE = 102400;
        public int updateDownlodedSize = 0;
        public bool updateDone = false;
        private static FtpWebRequest CreateDownloadRequest(string file_name)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(SERVER_ADRESS+file_name); // запрос файла манифеста
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(SERVER_LOGIN, SERVER_PASSW);
            request.EnableSsl = true;

            return request;
        }
        public int CalculateUpdateSize()
        {
            int updtSize = 0;

            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
            delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                        System.Security.Cryptography.X509Certificates.X509Chain chain,
                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
            {
                return true; // **** Always accept
            };

            FtpWebRequest request = CreateDownloadRequest("Info.txt");

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())  // получение ответа и запись данных на диск
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        using (StreamWriter writer = new StreamWriter("Info.txt"))
                        {
                            while (reader.Peek() >= 0)
                            {
                                string s = reader.ReadLine() + "\n";
                                writer.Write(s);
                            }
                        }
                    }
                }
            }

            request = CreateDownloadRequest("Blacklist.txt");

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())  // получение ответа и запись данных на диск
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        using (StreamWriter writer = new StreamWriter("Blacklist.txt"))
                        {
                            while (reader.Peek() >= 0)
                            {
                                string s = reader.ReadLine() + "\n";
                                writer.Write(s);
                            }
                        }
                    }
                }
            }

            using (StreamReader reader = new StreamReader("Info.txt"))
            {
                while (reader.Peek() >=0)
                {
                    string s = reader.ReadLine();
                    string[] fileInfo = s.Split('|');
                    updtSize += Convert.ToInt32(fileInfo[2]);
                }
            }
            return updtSize;
        }

        public bool CheckVersion()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
            delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                        System.Security.Cryptography.X509Certificates.X509Chain chain,
                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
            {
                return true; // **** Always accept
            };

            FtpWebRequest request = CreateDownloadRequest("Version.txt");

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())  // получение версии
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        try
                        {
                            StreamReader version = new StreamReader("Version.txt");

                            string s = reader.ReadLine();
                            if (s != version.ReadLine())
                            {
                                version.Close();
                                StreamWriter newVersion = new StreamWriter("Version.txt");
                                newVersion.WriteLine(s);
                                newVersion.Close();
                                return true;
                            }

                            else { version.Close(); return false; }
                                
                            
                        }
                        catch(FileNotFoundException e)
                        {
                            string s = reader.ReadLine();
                            StreamWriter newVersion = new StreamWriter("Version.txt");
                            newVersion.WriteLine(s);
                            newVersion.Close();
                            return true;
                        }
                    }
                }
            }
        }
        private void checkFolder(string folder)
        {
            string[] list_folders = Directory.GetDirectories(folder);

            foreach (string f in list_folders)
            {
                bool flag = false;
                using (StreamReader bl = new StreamReader("Blacklist.txt"))
                {
                    while (bl.Peek() > 0)
                    {
                        string s = bl.ReadLine();
                        string foldername = f.Remove(0, curFolder.Length + GAME_FOLDER.Length + 1);
                        if(s == foldername)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                if (!flag)
                    checkFolder(f);

            }



            string[] list_files = Directory.GetFiles(folder);
            foreach (string f in list_files)
            {
                bool flag = false;
                using (StreamReader info = new StreamReader("Info.txt"))
                {
                    while (info.Peek() > 0)
                    {
                        string s = info.ReadLine();
                        string[] fileInfo = s.Split('|');
                        string filename = f.Remove(0, curFolder.Length + GAME_FOLDER.Length + 1);
                        if(filename == fileInfo[0])
                        {
                            flag = true;
                            break;
                        }
                    }

                }
                if (!flag)
                {
                    using (StreamReader bl = new StreamReader("Blacklist.txt"))
                    {
                        while(bl.Peek() > 0)
                        {
                            string s = bl.ReadLine();
                            string filename = f.Remove(0, curFolder.Length + GAME_FOLDER.Length + 1);
                            if (filename == s)
                            {
                                flag = true;
                                break;
                            }
                        }

                    }
                }
                if (!flag)
                {
                    File.Delete(f);
                }
            }
            
        }
        public void Cleanup() 
        {
            if (!File.Exists("Blacklist.txt"))
            {
                var f = File.Create("Blacklist.txt");
                f.Close();
            }
            curFolder = Directory.GetCurrentDirectory();
            checkFolder(curFolder + "\\" + GAME_FOLDER);
            File.Delete("Info.txt");
        }
        public void UpdateGame()
        {
            updateDone = false;
            updateDownlodedSize = 0;
            //мой сервер имел битый сертификат поэтому пришлось насильно заставить принять его, надо бы избавиться
            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
            delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                        System.Security.Cryptography.X509Certificates.X509Chain chain,
                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
            {
                return true; // **** Always accept
            };


            using (var md5 = MD5.Create())                                      // проверяем файлы с компа и на сервере через мд5
            {
                using (StreamReader reader = new StreamReader("Info.txt"))
                {
                    while (reader.Peek() >= 0)
                    {
                        string s = reader.ReadLine();
                        string[] fileInfo = s.Split('|');

                        string[] path_tokens = fileInfo[0].Split('\\');             //отбрасываем название файла и оставляем только путь для создания папки
                        string path = "";
                        for (int i = 0; i < path_tokens.Length - 1; i++)
                            path += path_tokens[i] + "\\";
                        if (path.Length > 0)
                            Directory.CreateDirectory(GAME_FOLDER + path);

                        var file = File.Open(GAME_FOLDER + fileInfo[0], FileMode.OpenOrCreate);   //считаем мд5
                        var hash = md5.ComputeHash(file);
                        var existMD5 = BitConverter.ToString(hash).Replace("-", "");
                        file.Close();
                        if (existMD5 != fileInfo[1])                                //если не равны то так же как и манифест качаем файл
                        {
                            WebRequest requestgamefile = CreateDownloadRequest(fileInfo[0]);


                            using (var response = requestgamefile.GetResponse())
                            {
                                using (var responseStream = response.GetResponseStream())
                                {
                                    FileStream fs = new FileStream(GAME_FOLDER + fileInfo[0], FileMode.Create);
                                    byte[] buffer = new byte[4096];
                                    int size = 0;

                                    while ((size = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        fs.Write(buffer, 0, size);
                                    }
                                    
                                    fs.Close();
                                }
                            }

                            
                                
                        }
                        updateDownlodedSize += Convert.ToInt32(fileInfo[2]);

                    }
                }
                
                updateDone = true;
            }

        }
    }
}
