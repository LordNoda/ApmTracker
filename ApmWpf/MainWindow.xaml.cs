using Apm.Tracker;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace ApmWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ApmTracker apmTracker;
        public DispatcherTimer dispatcherTimer;

        public MainWindow()
        {
            InitializeComponent();

            //Initialise Apm Tracker
            apmTracker = new ApmTracker();

            //Initialise Event Timer
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

            //Initialise ListBox objects
            processData.ItemsSource = apmTracker.GetProcesses();
            autoDetectData.ItemsSource = apmTracker.GetPreviouslyRecordedProcesses();
        }

        private void trackButton_Click(object sender, RoutedEventArgs e)
        {
            if (!apmTracker.m_Running)
            {
                if (processData.SelectedItem != null)
                {
                    apmTracker.Subscribe(((KeyValuePair<System.IntPtr, string>)processData.SelectedItem).Value);
                }
            }
            else
            {
                apmTracker.Unsubscribe();
            }

        }

        //  System.Windows.Threading.DispatcherTimer.Tick handler
        //
        //  Updates the current seconds display and calls
        //  InvalidateRequerySuggested on the CommandManager to force 
        //  the Command to raise the CanExecuteChanged event.
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();

            if(autoDetect.IsChecked == true && !apmTracker.m_Running)
            {
                apmTracker.m_ApplcationName = (string)autoDetectData.SelectedItem;
                apmTracker.CheckSubscription(LiveCheckMode.Subscribe);
            }

            if (apmTracker.m_Running)
            {
                apmTracker.CalculateLiveApm();
                apmTracker.CalculateAverageApm();
                apmTracker.CheckSubscription(LiveCheckMode.Unsubscribe);
            }
            else
            {
                ApmHistoryTable.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = apmTracker.ApmHistorySet.Tables[0] });
            }

            trackButton.Content = apmTracker.m_Running ? "Stop" : "Start";
            apmLiveData.Text = apmTracker.m_Apm.ToString();
            apmAverageData.Text = apmTracker.m_AverageApm.ToString();
        }

        private void processData_Refresh(object sender, EventArgs e)
        {
            processData.ItemsSource = apmTracker.GetProcesses();
        }

        private void autoDetectData_Refresh(object sender, EventArgs e)
        {
            autoDetectData.ItemsSource = apmTracker.GetPreviouslyRecordedProcesses();
        }

        private void App_Exit(object sender, System.ComponentModel.CancelEventArgs e)
        {
            apmTracker.SaveHistoryTable();
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            apmTracker.ClearHistoryTable();
        }

        private void ClearRecord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                apmTracker.ClearRecordsHistoryTable(ApmHistoryTable.SelectedItems.Cast<DataRowView>().ToList());
            }
            catch (Exception ex)
            {
                // Do Nothing : This is to avoid the error of deleting the empty row
            }
        }

        private void autoDetect_Checked(object sender, RoutedEventArgs e)
        {
            //Disable and remove selected
            processData.IsEnabled = false;
            processData.SelectedItem = null;
            //Disable Start/Stop 
            trackButton.IsEnabled = false;
            //Enable detect dropdown
            autoDetectData.IsEnabled = true;

        }

        private void autoDetect_Unchecked(object sender, RoutedEventArgs e)
        {
            //Disable and remove selected
            autoDetectData.IsEnabled = false;
            autoDetectData.SelectedItem = null;
            //Enable Start/Stop 
            trackButton.IsEnabled = true;
            //Enable manual Process dropdown
            processData.IsEnabled = true;

        }
    }
    
}

