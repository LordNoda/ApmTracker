using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Apm.Tracker
{
    public class ApmTracker
    {
        private IKeyboardMouseEvents m_GlobalHook;
        public String m_ApplcationName;
        public int m_Actions { get; set; }
        public double m_Apm { get; set; }
        public double m_AverageApm { get; set; }
        public DateTime m_StartTime { get; set; }
        public DateTime m_LastCalculation { get; set; }
        public bool m_Running { get; set; }


        //Data Table (Running)
        private System.Data.DataSet ApmSet;
        private System.Data.DataTable ApmTable;
        private System.Data.DataColumn ApmTableCol1;
        private System.Data.DataColumn ApmTableCol2;

        //Data Table (History)
        public System.Data.DataSet ApmHistorySet;
        public System.Data.DataTable ApmHistoryTable;
        private System.Data.DataColumn ApmHistoryTableCol1;
        private System.Data.DataColumn ApmHistoryTableCol2;
        private System.Data.DataColumn ApmHistoryTableCol3;
        private System.Data.DataColumn ApmHistoryTableCol4;
        private System.Data.DataColumn ApmHistoryTableCol5;

        public ApmTracker(){

            CreateDataset();
            m_Actions = 0;
            m_Apm = 0;
            m_AverageApm = 0;
        }

        public void CreateDataset()
        {
            ApmHistorySet = new DataSet();
            if (!LoadHistoryTable() || ApmHistorySet.Tables.Count < 1)
            {
                ApmHistoryTable = new DataTable();

                ApmHistoryTableCol1 = new DataColumn();
                ApmHistoryTableCol2 = new DataColumn();
                ApmHistoryTableCol3 = new DataColumn();
                ApmHistoryTableCol4 = new DataColumn();
                ApmHistoryTableCol5 = new DataColumn();

                ApmHistoryTable.Columns.AddRange(new System.Data.DataColumn[] { ApmHistoryTableCol1, ApmHistoryTableCol2, ApmHistoryTableCol5, ApmHistoryTableCol3, ApmHistoryTableCol4 });
                ApmHistoryTable.TableName = "Apm History";

                ApmHistoryTableCol1.ColumnName = "Start Time";
                ApmHistoryTableCol1.DataType = typeof(string);

                ApmHistoryTableCol2.ColumnName = "End Time";
                ApmHistoryTableCol2.DataType = typeof(string);

                ApmHistoryTableCol3.ColumnName = "Process";
                ApmHistoryTableCol3.DataType = typeof(string);

                ApmHistoryTableCol4.ColumnName = "Average Apm";
                ApmHistoryTableCol4.DataType = typeof(string);

                ApmHistoryTableCol5.ColumnName = "Minutes";
                ApmHistoryTableCol5.DataType = typeof(string);

                ApmHistorySet.Tables.Add(ApmHistoryTable);
            }
        }


        public void Subscribe(string applicationName)
        {
            // Note: for the application hook, use the Hook.AppEvents() instead
            m_GlobalHook = Hook.GlobalEvents();
            m_ApplcationName = applicationName;

            m_GlobalHook.MouseDownExt += GlobalHookMouseDownExt;
            m_GlobalHook.KeyUp += GlobalHookKeyPress;

            m_Actions = 0;
            m_Apm = 0;
            m_AverageApm = 0;
            m_Running = true;
            m_StartTime = DateTime.Now;
            m_LastCalculation = DateTime.Now;

            //Set up DataTable
            ApmSet = new DataSet();
            ApmTable = new DataTable();
            ApmTableCol1 = new DataColumn();
            ApmTableCol2 = new DataColumn();

            ApmSet.DataSetName = "ApmSet";
            ApmSet.Tables.AddRange(new System.Data.DataTable[] {
            ApmTable});

            ApmTable.Columns.AddRange(new System.Data.DataColumn[] {
            ApmTableCol1,
            ApmTableCol2});
            ApmTable.TableName = "ApmTable";

            ApmTableCol1.ColumnName = "Minute";
            ApmTableCol1.DataType = typeof(int);

            ApmTableCol2.ColumnName = "Apm";
            ApmTableCol2.DataType = typeof(int);

        }

        public void CheckSubscription(LiveCheckMode liveCheckMode)
        {
            if(liveCheckMode == LiveCheckMode.Unsubscribe)
            {
                if (!Process.GetProcessesByName(m_ApplcationName).Any())
                {
                    Unsubscribe();
                }
            }
            //Used for autoDetection
            else if (liveCheckMode == LiveCheckMode.Subscribe)
            {
                if (Process.GetProcessesByName(m_ApplcationName).Any())
                {
                    Subscribe(m_ApplcationName);
                }
            }

        }

        private void GlobalHookKeyPress(object sender, KeyEventArgs e)
        {
            var foregroundProcess = GetForegroundWindow();
            var trackingProcess = Process.GetProcessesByName(m_ApplcationName);

            foreach (Process p in trackingProcess)
            {
                if (foregroundProcess == p.MainWindowHandle)
                {
                    m_Actions++;
                }
            }
        }

        private void GlobalHookMouseDownExt(object sender, MouseEventExtArgs e)
        {

            var foregroundProcess = GetForegroundWindow();
            var trackingProcess = Process.GetProcessesByName(m_ApplcationName);

            foreach (Process p in trackingProcess)
            {
                if (foregroundProcess == p.MainWindowHandle)
                {
                    m_Actions++;
                }
            }
        }

        public void Unsubscribe()
        {
            m_GlobalHook.MouseDownExt -= GlobalHookMouseDownExt;
            m_GlobalHook.KeyUp -= GlobalHookKeyPress;

            //It is recommened to dispose it
            m_GlobalHook.Dispose();

            //Set running to false
            m_Running = false;
            m_Apm = 0;
            AddToHistoryTable();

        }

        public void CalculateLiveApm()
        {
            //Calculate APM : (Actions / seconds taken) * 60 
            if (m_LastCalculation != DateTime.MinValue && m_StartTime != DateTime.MinValue)
            {
                var runTimeMinutes = DateTime.Now.Subtract(m_StartTime).TotalMinutes;
                var difference = DateTime.Now.Subtract(m_LastCalculation).TotalSeconds;
                var value = (int)((m_Actions / difference) * 60);

                m_Apm = value >= 0 ? value : 0;

                var newRow = ApmTable.NewRow();
                newRow["Minute"] = (int)runTimeMinutes;
                newRow["Apm"] = m_Apm;
                ApmTable.Rows.Add(newRow);

                m_Actions = 0;
                m_LastCalculation = DateTime.Now;
            }
        }

        public void CalculateAverageApm()
        {
            var values = ApmTable.AsEnumerable().Where(r => r.Field<int>("Minute") > 2).Select(r => r.Field<int>("Apm")).ToList();
            var sum = 0;

            for (var index = 0; index < values.Count; index++)
            {
                if(values[index] > 0)
                {
                    sum += values[index];
                }
            }

            if(values.Count > 0)
            {
                m_AverageApm = (int)(sum / values.Count);

            }
        }

        public Dictionary<System.IntPtr, string> GetProcesses()
        {

            var dictionary = new Dictionary<System.IntPtr, string>();
            var processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                if (!dictionary.ContainsKey(process.MainWindowHandle))
                {
                    dictionary.Add(process.MainWindowHandle, process.ProcessName);
                }
            }

            return dictionary;
        }

        public List<string> GetPreviouslyRecordedProcesses()
        {
            return ApmHistorySet.Tables[0].AsEnumerable().Select(d => d.Field<string>("Process")).Distinct().ToList();
        }

        public void AddToHistoryTable()
        {
            var newRow = ApmHistorySet.Tables[0].NewRow();
            newRow["Start Time"] = $"{m_StartTime}";
            newRow["End Time"] = $"{m_LastCalculation}";
            newRow["Minutes"] = $"{(int)(m_LastCalculation.Subtract(m_StartTime).TotalMinutes)}";
            newRow["Process"] = $"{m_ApplcationName}";
            newRow["Average Apm"] = m_AverageApm.ToString();
            ApmHistorySet.Tables[0].Rows.Add(newRow);
        }

        public void SaveHistoryTable()
        {
            ApmHistorySet.WriteXml("history.xml");
        }

        public bool LoadHistoryTable()
        {
            if (File.Exists("history.xml"))
            {
                ApmHistorySet.ReadXml("history.xml");
                return true;
            }
            return false;
        }

        public void ClearRecordsHistoryTable(List<DataRowView> dataRows)
        {
            dataRows.ForEach(a =>
            {
                a.Row.Delete();
            });
            SaveHistoryTable();
        }

        public void ClearHistoryTable()
        {
            ApmHistorySet.Clear();
            SaveHistoryTable();
            CreateDataset();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();
    }
}
