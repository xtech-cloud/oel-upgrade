using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace XTC.oelUpgrade
{
    public class Update
    {
        public enum Strategy
        {
            Ignore,
            Auto,
            Manual,
        }

        public class Entry
        {
            public string file { get; set; }
            public string md5 { get; set; }
            public long size { get; set; }
        }

        public class Repository
        {
            public Entry[] entry { get; set; }
            public string strategy { get; set; }
            public string host { get; set; }
        }

        public class Args
        {
            /// <summary>
            /// 源地址
            /// </summary>
            public string repository { get; set; }

            /// <summary>
            /// 目标文件夹
            /// </summary>
            public string targetDir { get; set; }
        }

        public Action<string> onFailure;
        public Action onSuccess;
        public Action<float, string> onStatus;

        private DownloaderManager downloadMgr { get; set; }
        private Args args { get; set; }
        private string cacheDir { get; set; }

        public Update()
        {

        }

        /// <summary>
        /// 拉取源
        /// </summary>
        /// <param name="_args"></param>
        /// <returns></returns>
        public void PullRpository(Args _args, Action<Repository> _onSuccess, Action<string> _onFailure)
        {
            args = _args;

            pullRepository(_args.repository, _onSuccess, _onFailure);
        }

        public int CompareFiles(Repository _repository)
        {
            cacheDir = Path.Combine(args.targetDir, ".upgrade");
            cacheDir = Path.Combine(cacheDir, "update");
            var tasks = generateTask(_repository);
            return tasks.Count;
        }


        public void Upgrade(Repository _repository)
        {
            cacheDir = Path.Combine(args.targetDir, ".upgrade");
            cacheDir = Path.Combine(cacheDir, "update");

            downloadMgr = new DownloaderManager();
            downloadMgr.onFinish = this.onDownloadFinish;
            downloadMgr.onError = this.onDownloadError;
            downloadMgr.onStatus = this.onDownloadStatus;
            downloadMgr.onUpdate = (_finish, _total) => { };
            downloadMgr.onTaskSuccess = (_task) =>
            {
                // 保存MD5值
                string md5;
                _task.metadata.TryGetValue("md5", out md5);
                if (string.IsNullOrEmpty(md5))
                    return;
                string file;
                _task.metadata.TryGetValue("file", out file);
                if (string.IsNullOrEmpty(file))
                    return;

                string md5_file = Path.Combine(cacheDir, file + ".md5");

                string dir = Path.GetDirectoryName(md5_file);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(md5_file, md5);
            };
            downloadMgr.onTaskFailure = (_task) =>
            {
                if (null != onFailure)
                    onFailure(_task.error);
            };

            var tasks = generateTask(_repository);

            Downloader.Options options = new Downloader.Options();
            downloadMgr.DownloadAsync(tasks, options);
        }


        private async void pullRepository(string _url, Action<Repository> _onSuccess, Action<string> _onFailure)
        {
            Repository repository = null;
            try
            {
                HttpWebRequest request = WebRequest.Create(_url) as HttpWebRequest;
                HttpWebResponse response = (await request.GetResponseAsync()) as HttpWebResponse;
                string content;
                using (Stream stream = response.GetResponseStream())
                {
                    content = new StreamReader(stream).ReadToEnd();
                }

                if (!string.IsNullOrEmpty(content))
                {
                    repository = JsonSerializer.Deserialize<Repository>(content);
                }
            }
            catch (Exception ex)
            {
                if (null != _onFailure)
                    _onFailure(ex.Message);
            }

            if (null != _onSuccess)
                _onSuccess(repository);
        }

        private void onDownloadFinish()
        {
            if (null != onSuccess)
                onSuccess();
        }

        private void onDownloadError(string _error)
        {

        }

        private void onDownloadStatus(float _progress, string _tip)
        {
            if (null != onStatus)
            {
                onStatus(_progress, _tip);
            }
        }

        private List<Dictionary<string, string>> generateTask(Repository _repository)
        {
            List<Dictionary<string, string>> tasks = new List<Dictionary<string, string>>();

            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);


            foreach (var entry in _repository.entry)
            {
                var task = new Dictionary<string, string>();
                string target_file = Path.Combine(args.targetDir, entry.file);
                string md5_file = Path.Combine(cacheDir, entry.file + ".md5");
                string local_md5 = "";
                // 仅当本地MD5文件存在且匹配时不需要下载
                if (File.Exists(target_file))
                {
                    if (File.Exists(md5_file))
                    {
                        local_md5 = File.ReadAllText(md5_file);
                        if (local_md5.Equals(entry.md5))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        local_md5 = getFileMD5(target_file);
                    }
                    // 保存本地文件的MD5值，提高下载失败后再次获取文件MD5的速度
                    string dir = Path.GetDirectoryName(md5_file);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    File.WriteAllText(md5_file, local_md5);
                    if (local_md5.Equals(entry.md5))
                        continue;
                }

                task["saveas"] = Path.Combine(args.targetDir, entry.file);
                task["url"] = string.Format("{0}/{1}", _repository.host, entry.file.Replace("\\", "/"));
                task["md5"] = entry.md5;
                task["meta.md5"] = entry.md5;
                task["meta.file"] = entry.file;
                task["meta.size"] = entry.size.ToString();
                tasks.Add(task);
            }
            return tasks;
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
