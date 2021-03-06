﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VRChatActivityLogViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<ActivityLogGridModel> ActivityLogGridModelCollection = new ObservableCollection<ActivityLogGridModel>();

        private readonly string errorFilePath = "./Logs/VRChatActivityLogger/errorfile.txt";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            DisableProcessingMode();

            fromDatePicker.SelectedDate = DateTime.Today.AddDays(-1);
            untilDatePicker.SelectedDate = DateTime.Today;
            ActivityLogGrid.ItemsSource = ActivityLogGridModelCollection;
        }

        /// <summary>
        /// Searchボタンクリック時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void searchButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteSearch();
        }

        /// <summary>
        /// 検索処理の実行
        /// </summary>
        /// <returns></returns>
        private async Task ExecuteSearch()
        {
            try
            {
                EnableProcessingMode();

                var parameter = new ActivityLogSearchParameter
                {
                    IsJoinedRoom = joinCheckBox.IsChecked ?? false,
                    IsMetPlayer = meetCheckBox.IsChecked ?? false,
                    IsSendInvite = sendInvCheckBox.IsChecked ?? false,
                    IsSendRequestInvite = sendReqInvCheckBox.IsChecked ?? false,
                    IsReceivedInvite = recvInvCheckBox.IsChecked ?? false,
                    IsReceivedRequestInvite = recvReqInvCheckBox.IsChecked ?? false,
                    IsSendFriendRequest = sendFriendReqCheckBox.IsChecked ?? false,
                    IsReceivedFriendRequest = recvFriendReqCheckBox.IsChecked ?? false,
                    IsAcceptFriendRequest = acptFriendReqCheckBox.IsChecked ?? false,
                    FromDateTime = fromDatePicker.SelectedDate,
                    UntilDateTime = untilDatePicker.SelectedDate?.AddDays(1),
                };
                var activityLogs = await VRChatActivityLogModel.SearchActivityLogs(parameter);

                var keywords = keywordBox.Text.Split(' ').Where(s => s != string.Empty).ToArray();

                ActivityLogGridModelCollection.Clear();

                foreach (var activityLog in activityLogs)
                {
                    var gridModel = new ActivityLogGridModel(activityLog);

                    if (keywords.Any())
                    {
                        var contained = keywords.All(k => gridModel.Content?.Contains(k, StringComparison.CurrentCultureIgnoreCase) ?? false);
                        if (!contained)
                        {
                            continue;
                        }
                    }

                    ActivityLogGridModelCollection.Add(gridModel);
                }

                DisableProcessingMode();
            }
            catch (Exception)
            {
                MessageBox.Show("エラーが発生しました。プログラムを終了します。", "VRChatActivityLogViewer");
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Loggerボタンクリック時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void loggerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnableProcessingMode();

                var success = await Task.Run(() =>
                {
                    var process = Process.Start("VRChatActivityLogger.exe");
                    process.WaitForExit();
                    return process.ExitCode == 0;
                });

                DisableProcessingMode();

                if (!success)
                {
                    if (File.Exists(errorFilePath))
                    {
                        var errorFileLines = File.ReadAllLines(errorFilePath);
                        if (0 < errorFileLines.Length)
                        {
                            var dialog = new LoggerErrorDialog(errorFileLines[0]);
                            dialog.Owner = this;
                            dialog.ShowDialog();

                            return;
                        }
                    }

                    MessageBox.Show("VRChatログの解析に失敗しました。", "VRChatActivityLogViewer");
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("VRChatActivityLogger.exeが見つかりませんでした。", "VRChatActivityLogViewer");
                DisableProcessingMode();
            }
            catch (Exception)
            {
                MessageBox.Show("エラーが発生しました。プログラムを終了します。", "VRChatActivityLogViewer");
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// WorldIDコピーボタンクリック時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyWorldIDButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Tag is ActivityLogGridModel tag)
                {
                    Clipboard.SetText(tag.WorldID ?? "");
                }
            }
        }

        /// <summary>
        /// UserIDコピーボタンクリック時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyUserIDButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Tag is ActivityLogGridModel tag)
                {
                    Clipboard.SetText(tag.UserID ?? "");
                }
            }
        }

        /// <summary>
        /// Joinボタンクリック時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Tag is ActivityLogGridModel tag)
                {
                    var uri = "vrchat://launch?id=" + tag.WorldID;
                    uri = uri.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {uri}") { CreateNoWindow = true });
                }
            }
        }

        /// <summary>
        /// 処理中モードにする
        /// </summary>
        private void EnableProcessingMode()
        {
            taskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
            Mouse.OverrideCursor = Cursors.Wait;
            loggerButton.IsEnabled = false;
            searchButton.IsEnabled = false;
        }

        /// <summary>
        /// 処理中モードを解除する
        /// </summary>
        private void DisableProcessingMode()
        {
            taskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            Mouse.OverrideCursor = null;
            loggerButton.IsEnabled = true;

            if (File.Exists(DatabaseContext.DBFilePath))
            {
                searchButton.IsEnabled = true;
            }
            else
            {
                searchButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// キーワード入力時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void keywordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await ExecuteSearch();
            }
        }
    }
}
