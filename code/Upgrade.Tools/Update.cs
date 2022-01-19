using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Upgrade.Tools
{
    public class Update
    {
        public class Entry
        {
            public string path { get; set; }
            public string hash { get; set; }
            public string url { get; set; }
            public long size { get; set; }
        }

        public class Repository
        {
            public List<Entry> entry { get; set; }
            public string strategy { get; set; }
            public string host { get; set; }
            public string key { get; set; }
        }

        public void GenerateRepository(string _targetDir)
        {
            Repository repository = new Repository();
            repository.strategy = "Auto";
            repository.host = "";
            repository.key = "path";
            repository.entry = new List<Entry>();
            foreach (var file in Directory.GetFiles(_targetDir, "*", SearchOption.AllDirectories))
            {
                FileInfo fi = new FileInfo(file);
                Entry entry = new Entry();
                entry.path = Path.GetRelativePath(_targetDir, file);
                entry.size = fi.Length;
                entry.hash = getFileMD5(file);
                entry.url = "";
                repository.entry.Add(entry);
            }

            string json = JsonSerializer.Serialize(repository);
            File.WriteAllText(Path.Combine(_targetDir, "repo.update.json"), json);
        }

        private string getFileMD5(string _file)
        {
            FileStream file = new FileStream(_file, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
