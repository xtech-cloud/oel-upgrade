using System.IO;
using System.Collections.Generic;
using System.Net;
using System.ComponentModel;
using System;
using System.Diagnostics;
using System.Threading;

namespace XTC.oelUpgrade
{
    // 添加超时机制的WebClient扩展
    public class WebClientExt : WebClient
    {
        private const int TIMEOUT = 10;
        private DateTime startTime_;
        private TimeSpan timeout_ = new TimeSpan(0, 0, TIMEOUT);
        private bool isStartTimeoutCheck_ = false;
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.Timeout = 1000 * TIMEOUT;
            request.ReadWriteTimeout = 1000 * TIMEOUT;
            return request;
        }

        public void DownloadFileAsyncWithTimeout(Uri _address, string _filename, object _userToken)
        {
            this.DownloadProgressChanged += new DownloadProgressChangedEventHandler(handleDownloadProgressChanged);
            startTimeoutCheck();
            DownloadFileAsync(_address, _filename, _userToken);
        }

        private void handleDownloadProgressChanged(object _sender, DownloadProgressChangedEventArgs _e)
        {
            //当下载进度有变化时重置超时时间
            resetTimeoutCheck();
        }

        private void startTimeoutCheck()
        {
            resetTimeoutCheck();
            isStartTimeoutCheck_ = true;
            Thread th = new Thread(runTimeoutCheck);
            th.IsBackground = true;
            th.Start();
        }

        private void stopTimeoutCheck()
        {
            isStartTimeoutCheck_ = false;
        }

        private void resetTimeoutCheck()
        {
            startTime_ = DateTime.Now;
        }

        private void runTimeoutCheck()
        {
            try
            {
                bool isTimeout = (DateTime.Now - startTime_).Seconds >= TIMEOUT;
                while (isStartTimeoutCheck_ && !isTimeout)
                {
                    Thread.Sleep(1000);
                }
                //已超时，取消下载 
                this.CancelAsync();
            }
            catch (Exception)
            {
                stopTimeoutCheck();
            }

        }
    }

    public class Downloader
    {
        public class Options
        {
            /// <summary>
            /// 生成md5文件
            /// </summary>
            public bool generateMd5File { get; set; }
        }

        public class Task
        {
            public string filepath { get; set; }
            public string url { get; set; }
            public bool finish { get; set; }
            public string format { get; set; }
            public string error { get; set; }
            public Options options { get; set; }
        }

        public Action<Task> onSuccess;
        public Action<Task> onFailure;
        public Action<float, string> onUpdate;

        private Stopwatch sw_;
        private WebClientExt webClient_;
        private Task task_;

        public void Download(Task _task)
        {
            task_ = _task;
            task_.finish = false;
            asyncDownload(_task.url, _task.filepath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_url">下载文件的url</param>
        /// <param name="_filepath">下载文件保存路径</param>
        internal void asyncDownload(string _url, string _filepath)
        {
            string dir = Path.GetDirectoryName(_filepath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (string.IsNullOrEmpty(_url))
            {
                task_.finish = true;
                onSuccess(task_);
                return;
            }

            // 已存在已下载的文件
            if (File.Exists(_filepath + ".md5"))
            {
                string md5 = File.ReadAllText(_filepath + ".md5");
                //TODO md5判断
                if (md5.Equals(_url))
                {
                    task_.finish = true;
                    onSuccess(task_);
                    return;
                }
            }

            sw_ = new Stopwatch();
            using (webClient_ = new WebClientExt())
            {
                webClient_.DownloadFileCompleted += new AsyncCompletedEventHandler(onCompleted);
                webClient_.DownloadProgressChanged += new DownloadProgressChangedEventHandler(onProgressChanged);
                try
                {
                    Uri url = new Uri(_url);
                    webClient_.DownloadFileAsync(url, _filepath + ".tmp");
                }
                catch (System.Exception ex)
                {
                    task_.error = ex.Message;
                    onFailure(task_);
                }
            }
        }

        private void onCompleted(object _sender, AsyncCompletedEventArgs _e)
        {
            sw_.Reset();
            task_.finish = !_e.Cancelled;
            if (_e.Cancelled)
            {
                task_.error = "Download has been canceled.";
                onFailure(task_);
                return;
            }

            //保存文件md5
            if (task_.options.generateMd5File)
            {
                //File.WriteAllText(task_.filepath + ".md5", task_.url);
            }
            try
            {
                File.Move(task_.filepath + ".tmp", task_.filepath, true);
            }
            catch (System.Exception ex)
            {
                onFailure(task_);
                return;
            }
            onSuccess(task_);
        }

        private void onProgressChanged(object _sender, DownloadProgressChangedEventArgs _e)
        {
            string speed = string.Format("{0} kb/s", (_e.BytesReceived / 1024d / sw_.Elapsed.TotalSeconds).ToString("0.00"));
            float progress = _e.ProgressPercentage;
            //labelPerc.Text = e.ProgressPercentage.ToString() + "%";
            string status = string.Format("{0} MB / {1} MB",
                (_e.BytesReceived / 1024d / 1024d).ToString("0.00"),
               (_e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"));
            string msg = string.Format("{0}    {1}", speed, status);
            onUpdate(progress, msg);
        }
    }

    public class DownloaderManager
    {
        public Action onFinish;
        public Action<string> onError;
        public Action<int, int> onUpdate;
        public Action<float, string> onStatus;

        private Queue<Downloader.Task> tasks_;
        private Queue<Downloader.Task> retryTasks_;
        private int finished_;
        private int total_;

        public void DownloadAsync(List<Dictionary<string, string>> _tasks, Downloader.Options _options)
        {
            tasks_ = new Queue<Downloader.Task>();
            retryTasks_ = new Queue<Downloader.Task>();
            foreach (var e in _tasks)
            {
                Downloader.Task task = new Downloader.Task();
                task.options = _options;
                task.filepath = e["saveas"];
                task.url = e["url"];
                tasks_.Enqueue(task);
            }
            total_ = tasks_.Count;
            finished_ = 0;
            downloadSingle();
        }

        private void downloadSingle()
        {
            if (tasks_.Count == 0)
            {
                if (retryTasks_.Count == 0)
                    onFinish();
                else
                    onError(string.Format("has {0} task need retry!", retryTasks_.Count));
                return;
            }

            var task = tasks_.Dequeue();
            var downloader = new Downloader();
            downloader.onSuccess = (_task) =>
             {
                 if (_task.finish)
                 {
                     finished_ += 1;
                 }
                 else
                 {
                     //下载失败，重新入队
                     //直接放回task_将引起堆栈溢出异常
                     //tasks_.Enqueue(_task);
                     retryTasks_.Enqueue(_task);
                 }
                 onUpdate(finished_, total_);
                 downloadSingle();
             };
            downloader.onUpdate = (_progress, _status) =>
            {
                onStatus(_progress, _status);
            };
            downloader.Download(task);
        }
    }
}
