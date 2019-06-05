using System;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 日志文件
    /// </summary>
    public class FileLogger : Logger
    {
        /// <summary>
        /// 文件名 
        /// </summary>
        private readonly string _name;

        /// <summary>
        /// 日期 
        /// </summary>
        private DateTime _date;

        /// <summary>
        /// 文件保存目录 
        /// </summary>
        private readonly string _directory;

        /// <summary>
        /// 日志保存天数
        /// </summary>
        private readonly int _holdDays;

        /// <summary>
        /// 文件流 
        /// </summary>
        private FileStream _fs;

        /// <summary>
        /// 文件流写入
        /// </summary>
        private StreamWriter _sw;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="minLevel">日志筛选最低级别</param>
        /// <param name="maxLevel">日志筛选最高级别</param>
        /// <param name="name">日志名称</param>
        public FileLogger(LogLevel minLevel, LogLevel maxLevel, string name)
            :base(minLevel,maxLevel)
        {
            _name = name;
            _date= DateTime.Today;

            //读取文件日志参数
            _directory = LogConfig.LogDirectory;
            _holdDays = LogConfig.LogHoldDays;

            //创建文件目录和删除超时文件
            if (Directory.Exists(_directory))
            {
                DeleteFile();
            }
            else
            {
                Directory.CreateDirectory(_directory);
            }

            _fs = new FileStream(
                Path.Combine(_directory, $"{_name}_{_date:yyMMdd}.log"),
                FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _sw = new StreamWriter(_fs);

        }

        /// <summary>
        /// 根据当前日期和日志保存时间删除过期的日志
        /// </summary>
        private void DeleteFile()
        {
            foreach (string filePath in Directory.GetFiles(_directory))
            {
                string[] datas = Path.GetFileNameWithoutExtension(filePath)
                    .Split("_", StringSplitOptions.RemoveEmptyEntries);
                if (datas.Length >= 2)
                {
                    if (DateTime.TryParseExact(datas[datas.Length - 1], "yyMMdd", CultureInfo.CurrentCulture,
                        DateTimeStyles.None, out DateTime fileDate))
                    {
                        if (_name == datas[0] && (DateTime.Today - fileDate).TotalDays >= _holdDays)
                        {
                            try
                            {
                                File.Delete(filePath);
                            }
                            catch (IOException)
                            {

                            }
                        }
                    }
                }
            }
        }

        public void Close()
        {
            if (_sw != null && _fs != null)
            {
                _sw.Close();
                _fs.Close();
            }
        }

        protected override void LogCore(string log)
        {
            if (_date != DateTime.Today)
            {
                Close();

                DeleteFile();

                _date = DateTime.Today;
                _fs = new FileStream(
                    Path.Combine(_directory, $"{_name}_{_date:yyMMdd}.log"),
                    FileMode.Append,FileAccess.Write,FileShare.ReadWrite);
                _sw = new StreamWriter(_fs, Encoding.UTF8);
            }
            _sw.WriteLine(log);
            _sw.Flush();
        }

    };
}
