﻿using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NewLife.Compression;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;

namespace NewLife.Extension
{
    class SpeakProvider
    {
        private const string typeName = "Microsoft.Speech.Synthesis.SpeechSynthesizer";
        private Type _type;

        public SpeakProvider()
        {
            //AssemblyX.AssemblyPaths.Add("C:\\X\\");
            _type = typeName.GetTypeEx(true);
            if (_type == null)
            {
                var file = "Microsoft.Speech.dll";
                if (Runtime.IsWeb) file = "Bin".CombinePath(file);
                file = file.EnsureDirectory();

                if (!File.Exists(file))
                {
                    var url = "http://www.newlifex.com/showtopic-51.aspx";
                    XTrace.WriteLine("没有找到语音驱动库，准备联网获取 {0}", url);

                    var client = new WebClientX(true, true);
                    var dir = Path.GetDirectoryName(file);
                    var sw = new Stopwatch();
                    sw.Start();
                    var file2 = client.DownloadLink(url, "Microsoft.Speech", dir);
                    sw.Stop();

                    if (!file2.IsNullOrEmpty())
                    {
                        XTrace.WriteLine("下载完成，共{0:n0}字节，耗时{1}毫秒", file2.AsFile().Length, sw.ElapsedMilliseconds);

                        ZipFile.Extract(file2, dir);

                        // 尝试加载，如果成功，则说明已经安装运行时，仅仅缺类库
                        LoadType(file);
                    }

                }
                else
                    LoadType(file);
            }
            CheckVoice();
        }

        Boolean LoadType(String file)
        {
            if (!File.Exists(file)) return false;

            var assembly = Assembly.LoadFrom(file);
            if (assembly == null) return false;

            _type = assembly.GetType(typeName);
            if (_type == null) return false;

            return true;
        }

        /// <summary>检查是否安装了语音库</summary>
        void CheckVoice()
        {
            if (_type == null) return;

            var synth = _type.CreateInstance(new Object[0]);
            var vs = synth.Invoke("GetInstalledVoices") as IList;
            if (vs != null && vs.Count > 0)
            {
                var flag = false;
                foreach (var item in vs)
                {
                    XTrace.WriteLine("语音库：{0}", item.GetValue("VoiceInfo").GetValue("Description"));

                    if ((Boolean)item.GetValue("Enabled")) flag = true;
                }
                if (flag) return;
            }

            var url = "http://www.newlifex.com/showtopic-51.aspx";
            XTrace.WriteLine("没有找到语音运行时，准备联网获取 {0}", url);

            var dir = ".".GetFullPath();
            if (Runtime.IsWeb) dir = dir.CombinePath("Bin");
            dir.EnsureDirectory();

            var client = new WebClientX(true, true);
            var sw = new Stopwatch();
            sw.Start();
            var file2 = client.DownloadLink(url, "SpeechRuntime", dir);
            sw.Stop();

            if (!file2.IsNullOrEmpty())
            {
                XTrace.WriteLine("下载完成，共{0:n0}字节，耗时{1}毫秒", file2.AsFile().Length, sw.ElapsedMilliseconds);

                ZipFile.Extract(file2, dir);

                // 安装语音库
                var msi = "SpeechPlatformRuntime_x{0}.msi".F(Runtime.Is64BitOperatingSystem ? 64 : 86);
                msi = dir.CombinePath(msi);
                if (File.Exists(msi)) "msiexec".Run("/i \"" + msi + "\" /passive /norestart", 5000);

                msi = "MSSpeech_TTS_zh-CN_HuiHui.msi";
                msi = dir.CombinePath(msi);
                if (File.Exists(msi)) "msiexec".Run("/i \"" + msi + "\" /passive /norestart", 5000);
            }
        }

        private object synth;
        public void SpeakAsync(String value)
        {
            if (_type == null) return;

            if (synth == null)
            {
                try
                {
                    synth = _type.CreateInstance(new object[0]);
                    synth.Invoke("SetOutputToDefaultAudioDevice", new object[0]);
                }
                catch (Exception ex)
                {
                    var msi = "SpeechPlatformRuntime_x{0}.msi".F(Runtime.Is64BitOperatingSystem ? 64 : 86);
                    XTrace.WriteLine("加载语音模块异常，可能未安装语音运行时{0}！", msi);
                    XTrace.WriteException(ex);
                    _type = null;
                }
            }
            if (synth != null) synth.Invoke("SpeakAsync", value);
        }
    }
}