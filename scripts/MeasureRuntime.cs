using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
namespace KinetixStudio {
    [DataContract] public class RuntimeMeasurement {
        [DataMember] public string Scenario;
        [DataMember] public double Seconds, CpuOneCorePercent, CpuMachinePercent, AverageWorkingSetMiB, AveragePrivateMiB;
        [DataMember] public int Samples, GdiStart, GdiEnd, GdiPeak, UserStart, UserEnd;
        [DataMember] public long Reports;
        [DataMember] public bool Minimized;
    }
    public static class MeasureRuntime {
        [DllImport("user32.dll")] static extern int GetGuiResources(IntPtr process,int flag);
        [STAThread] public static void Main(string[] args) {
            Environment.SetEnvironmentVariable("KINETIX_DATA_DIR",Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data"));
            Native.SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            List<RuntimeMeasurement> results=new List<RuntimeMeasurement>();
            foreach(string scenario in args.Length>1?new string[]{args[1]}:new string[]{"idle-visible","idle-minimized","active-neutral-minimized","navigation"}) {
                Profile p=Profile.Create();p.Name="Runtime benchmark";p.GamePath=Process.GetCurrentProcess().MainModule.FileName;Store.Save(p);
                using(MainWindow form=new MainWindow(false)) using(Process process=Process.GetCurrentProcess()) using(System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer()) {
                    InputEngine engine=(InputEngine)typeof(MainWindow).GetField("engine",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
                    RuntimeMeasurement row=new RuntimeMeasurement{Scenario=scenario};long cpuStart=0;Stopwatch clock=new Stopwatch();double sumWs=0,sumPrivate=0;int count=0;bool initialized=false;int warmup=0;
                    form.Shown+=(s,e)=>{if(scenario.Contains("minimized"))form.WindowState=FormWindowState.Minimized;};
                    timer.Interval=250;
                    timer.Tick+=(s,e)=>{
                        if(!initialized){warmup++;if(warmup==7&&scenario.Contains("minimized"))form.WindowState=FormWindowState.Minimized;if(warmup<8)return;if(scenario.StartsWith("active"))engine.Start(p);process.Refresh();cpuStart=process.TotalProcessorTime.Ticks;row.GdiStart=GetGuiResources(process.Handle,0);row.UserStart=GetGuiResources(process.Handle,1);clock.Start();initialized=true;return;}
                        if(scenario=="navigation")form.Navigate(count%7);
                        process.Refresh();sumWs+=process.WorkingSet64;sumPrivate+=process.PrivateMemorySize64;row.GdiPeak=Math.Max(row.GdiPeak,GetGuiResources(process.Handle,0));count++;
                        if(count<32)return;
                        clock.Stop();row.Seconds=clock.Elapsed.TotalSeconds;row.CpuOneCorePercent=(process.TotalProcessorTime.Ticks-cpuStart)/(double)TimeSpan.TicksPerSecond/row.Seconds*100;row.CpuMachinePercent=row.CpuOneCorePercent/Environment.ProcessorCount;row.AverageWorkingSetMiB=sumWs/count/1048576;row.AveragePrivateMiB=sumPrivate/count/1048576;row.Samples=count;row.GdiEnd=GetGuiResources(process.Handle,0);row.UserEnd=GetGuiResources(process.Handle,1);row.Reports=engine.ReportsSent;row.Minimized=form.WindowState==FormWindowState.Minimized;results.Add(row);timer.Stop();form.Close();
                    };
                    timer.Start();Application.Run(form);
                }
            }
            using(FileStream output=File.Create(args[0]))new DataContractJsonSerializer(typeof(List<RuntimeMeasurement>)).WriteObject(output,results);
        }
    }
}
