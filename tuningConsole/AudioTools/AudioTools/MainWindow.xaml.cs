using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Net;
using Microsoft.Win32;

namespace AudioTools
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {

        //private String rsa_key_path = @"c:\";
        SshConnectHelper client = null;
        private ArrayList EffectCandidate = new ArrayList();

        public MainWindow()
        {
            InitializeComponent();
            initUI();
        }



        private void initUI()
        {
            txBox_password.IsEnabled = false;
            txBox_username.IsEnabled = false;
            txBox_ipaddress.IsEnabled = true;
        }

        private void KeyboardAuthentication_AuthenticationPrompt(object sender, Renci.SshNet.Common.AuthenticationPromptEventArgs e)
        {
            String cb_pwd = "test0000";
            foreach (AuthenticationPrompt prompt in e.Prompts)
            {
                if (prompt.Request.StartsWith("Password"))
                    prompt.Response = cb_pwd;           // Hard coded here just for the sample
                else if (prompt.Request.StartsWith("Verification"))
                    prompt.Response = "...";                // Insert Verification code at runtime using here a Debugger breakpoint
            }
        }

        private void Btn_toggle_connect_Click (object sender, RoutedEventArgs e)
        {
            btn_toggle_connect.IsEnabled = false;
            if (client != null)
            {
                client.SshDisconnect();
                client = null;
                btn_toggle_connect.Content = "Connect";
            }
            else
            {
                try
                {
                    IPAddress ipaddrss;
                    if (IPAddress.TryParse(txBox_ipaddress.Text.ToString(), out ipaddrss))
                    {
                        client = new SshConnectHelper(ipaddrss.ToString());
                        btn_toggle_connect.Content = client.SshConnect() ? "Disconnect" : "Connect";
                    }
                    else
                    {
                        MessageBox.Show("Not vaild id address format");
                    }
                }
                catch(Exception exception)
                {
                    Console.WriteLine(exception.Message.ToString());
                }
                
            }
            updateInformation();
            btn_toggle_connect.IsEnabled = true;
        }

        private void updateInformation()
        {
            if (client != null && client.IsConnected())
            {
                lab_cb_project_code.Content     =   client.CbProjectCode;
                lab_cras_path.Content           =   client.CrasPath;
                lab_os_version.Content          =   client.CbOSVersion;
                lab_ucm_suffix.Content          =   client.UcmSuffix;
                lab_effect_status.Content       +=  client.EffectStatus;
            }
            else
            {
                string dash = "-------------------------------------------------------------------------------";
                lab_cb_project_code.Content         = dash;
                lab_cras_path.Content               = dash;
                lab_os_version.Content              = dash;
                lab_ucm_suffix.Content              = dash;
                lab_effect_status.Content           = dash;
            }
        }

        private void Btn_update_volume_table_Click(object sender, RoutedEventArgs e)
        {
            
            if (txBox_file_candidate_path.Text != string.Empty)
            {
                client.UpdateVolumeTable(txBox_file_candidate_path.Text);
            }
        }

        private void Btn_pickup_volume_table_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "volume table (*.card_settings)|*.card_settings",
                RestoreDirectory = true
            };
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                txBox_file_candidate_path.Text = string.Empty;
                txBox_file_candidate_path.Text = openFileDialog.FileName;
            }
        }

        private void Btn_toggle_effect_Click(object sender, RoutedEventArgs e)
        {
            client.ToggleEffect();
        }

        private void Btn_pick_effects_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Waves Effect (*.bin)| waves_params_*.bin",
                RestoreDirectory = true
            };
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                tbx_effect_candidate_files.Text = string.Empty;
                EffectCandidate.Clear();
                foreach (string files in openFileDialog.FileNames)
                {
                    tbx_effect_candidate_files.Text += files + '\n';
                    EffectCandidate.Add(files);
                }
            }
        }

        private void Btn_update_effects_Click(object sender, RoutedEventArgs e)
        {
            if (EffectCandidate.Count == 0) return;
            client.UpdateEffect(EffectCandidate);
        }

        private void ConsoleWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (client != null)
            {
                client.SshDisconnect();
                client = null;
            }
        }

    }
}
