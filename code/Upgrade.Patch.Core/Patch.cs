using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.IO.Compression;

namespace XTC.oelUpgrade
{
    public class Patch
    {
        public enum Strategy
        {
            Ignore,
            Auto,
            Manual,
        }

        public class Repository
        {
            public string version { get; set; }
            public long size { get; set; }
            public string url { get; set; }
            public string[] tag { get; set; }
            public string note { get; set; }
            public string strategy { get; set; }
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

        public Patch()
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

        /// <summary>
        /// 对比版本
        /// </summary>
        /// <param name="_localVersion"></param>
        /// <param name="_remoteVersion"></param>
        /// <return>
        /// -1:小于，0:相等，1：大于
        /// </return>
        public int CompareVersion(string _localVersion, string _remoteVersion)
        {
            int result = 0;
            string[] localVersion = _localVersion.Split(".");
            string[] remoteVersion = _remoteVersion.Split(".");
            for (int i = 0; i < localVersion.Length && i < remoteVersion.Length; i++)
            {
                int localNumber = int.Parse(localVersion[i]);
                int remoteNumber = int.Parse(remoteVersion[i]);
                if (localNumber < remoteNumber)
                {
                    result = -1;
                    break;
                }
                if (localNumber > remoteNumber)
                {
                    result = 1;
                    break;
                }
            }
            return result;
        }

        public void Upgrade(Repository _repository)
        {
            downloadMgr = new DownloaderManager();
            downloadMgr.onFinish = this.onDownloadFinish;
            downloadMgr.onError = this.onDownloadError;
            downloadMgr.onStatus = this.onDownloadStatus;
            downloadMgr.onUpdate = (_finish, _total) => { };
            List<Dictionary<string, string>> tasks = new List<Dictionary<string, string>>();
            var task = new Dictionary<string, string>();
            task["saveas"] = Path.Combine(args.targetDir, "patch.zip");
            task["url"] = _repository.url;
            tasks.Add(task);
            Downloader.Options options = new Downloader.Options();
            options.generateMd5File = false;
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
            string patchfile = Path.Combine(args.targetDir, "patch.zip");
            ZipFile.ExtractToDirectory(patchfile, args.targetDir, true);
            File.Delete(patchfile);
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

    }
}
