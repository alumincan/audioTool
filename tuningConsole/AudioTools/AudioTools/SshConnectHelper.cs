using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Collections;

public class SshConnectHelper
{
    private SshClient SClient;
    private string Ip_Address                       = "192.168.0.100";
    private string TestAccount                      = "root";
    private string TestPassword                     = "test0000";
    private int ConnectRetryTimes                   = 3;


    private string _crasPath                         = "";
    private string _ucmSuffix                        = "";
    private string _cbProjectCode                    = "";
    private string _cbOSVersion                      = "";
    private string _audioSoundCard                   = "";
    private string _effectStatus                     = "";
    

    private const string CMD_CRAS_START             = "restart cras";
    private const string CMD_CRAS_PARAMETER         = "ps -C cras -o cmd";
    private const string CMD_OS_SYNC                = "sync";
    private const string CMD_GET_PROJECT_CODE       = "cros_config / name";
    private const string CMD_GET_OS_VERSION         = "cat /etc/lsb-release | grep RELEASE_VERSION | cut -d = -f 2";
    private const string CMD_GET_CRAS_CONFIG        = "cros_config /audio/main cras-config-dir";
    private const string CMD_GET_UCM_SUFFIX         = "cros_config /audio/main ucm-suffix";
    private const string CMD_GET_SOUND_CARD         = "cat /proc/asound/cards | cut -d : -f 2 | cut -d \" \" -f 2 ";
    private const string CMD_CHECK_ORI_VALUME_TABLE = "cros_config /audio/main ucm-suffix";

    private const string EFFECT_HEADPHONE_BYPASS    = "waves_params_headphone_bypass.bin";
    private const string EFFECT_SPEAKER_BYPASS      = "waves_params_speaker_bypass.bin";
    private const string EFFECT_SPEAKER             = "waves_params_speaker.bin";
    private const string EFFECT_HEADPHONE           = "waves_params_headphone.bin";

    private const string PATH_ETC_CRAS              = "/etc/cras/";
    private const string PATH_ALSA_UCM              = "/usr/share/alsa/ucm/";
    private const string FILE_HIFI_CONF             = "HiFi.conf";

    private const string FILE_VOLUME_TABLE_NAME     = "sof-rt5682.card_settings";
    private const string FILE_ORI_VOLUME_TABLE_NAME = "ori.sof-rt5682.card_settings";
    private string RENAME_FORMAT                    = "rename {0}{1} {0}{2} {0}*.{3}";
    private string CP_FORMAT                        = "cp {0} {1}";

    
    

    public string CrasPath { get => _crasPath; }
    public string UcmSuffix { get => _ucmSuffix;}
    public string CbProjectCode { get => _cbProjectCode; set => _cbProjectCode = value; }
    public string CbOSVersion { get => _cbOSVersion; set => _cbOSVersion = value; }
    public string AudioSoundCard { get => _audioSoundCard; }
    public string EffectStatus { get => _effectStatus; }

    public SshConnectHelper()
	{
    }
    
    public SshConnectHelper(string ip_address)
    {
        this.Ip_Address = ip_address;
    }

    public bool IsConnected()
    {
        if (SClient != null) {
            return SClient.IsConnected;
        }
        return false;
    }

    public bool SshConnect()
    {

        if (SClient == null)
        {
            KeyboardInteractiveAuthenticationMethod kAuth = new KeyboardInteractiveAuthenticationMethod(TestAccount);
            kAuth.AuthenticationPrompt += KeyboardAuthentication_AuthenticationPrompt;

            ConnectionInfo coninfor = new ConnectionInfo(this.Ip_Address, this.TestAccount, kAuth);
            SClient = new SshClient(coninfor);

            int retry = 0;
            while (!SClient.IsConnected)
            {
                if (++retry == 3)
                {
                    Console.WriteLine("Connect fail. Please help to check network connection.");
                    SClient = null;
                    break;
                }
                try
                {
                    SClient.Connect();
                }
                catch (SshAuthenticationException value)
                {
                    Console.WriteLine("Retry times =  " + retry.ToString());
                    Console.WriteLine(value.ToString());
                }
            }
        }
        GetSystemInfomation();
        return SClient.IsConnected;
    }

    public void SshDisconnect()
    {
        if (this.SClient != null && !this.SClient.IsConnected)
        {
            SClient.Disconnect();
            SClient.Dispose();
            SClient = null;
        }
    }

    private string UcmPath
    {
        get
        {
            StringBuilder ucm = new StringBuilder();
            ucm.Append(PATH_ALSA_UCM);
            ucm.Append(AudioSoundCard);
            ucm.Append(".");
            ucm.Append(UcmSuffix);
            ucm.Append("/");
            return ucm.ToString().Trim();
        }
    }


    private bool EffectInside(string hifiPath)
    {
        if (SClient.IsConnected)
        {
            return ExecCmd("grep CODEC_ADAPTER  " + hifiPath) == string.Empty ? false : true;
        }
        return false;
    }

    private bool IsEffectOn(string hifiPath)
    {
        if (SClient.IsConnected)
        {
            return ExecCmd("grep bypass " + hifiPath) == string.Empty ? true : false;
        }
        return false;
    }

    public bool ToggleEffect()
    {
        string ucm_file = UcmPath + FILE_HIFI_CONF;
        if (!EffectInside(ucm_file)) return false;

        string ori_ucm_file = UcmPath + "ori.HiFi.conf";
        string result = ExecCmd("ls " + ori_ucm_file).Trim();
        // Console.WriteLine(result.Result.ToString());
        if (result.Contains("No such file or directory") 
                || result == string.Empty )
        {
            ExecCmd(string.Format(CP_FORMAT, ucm_file, ori_ucm_file));
            System.Threading.Thread.Sleep(100);
        }

        if (IsEffectOn(ucm_file))
        {
            string stateful = "/mnt/stateful_partition/";
            ExecCmd(string.Format(CP_FORMAT, ucm_file, stateful));
            // SClient.RunCommand("cp " + ucm_file + " /mnt/stateful_partition/");
            System.Threading.Thread.Sleep(100);
            SClient.RunCommand("sed -i 's/waves_params_speaker.bin/waves_params_speaker_bypass.bin/g' /mnt/stateful_partition/HiFi.conf");
            System.Threading.Thread.Sleep(100);
            SClient.RunCommand("sed -i 's/waves_params_headphone.bin/waves_params_headphone_bypass.bin/g' /mnt/stateful_partition/HiFi.conf");
            System.Threading.Thread.Sleep(100);
            SClient.RunCommand("cp /mnt/stateful_partition/HiFi.conf " + ucm_file);
            System.Threading.Thread.Sleep(100);
        }
        else
        {
            ExecCmd(string.Format(CP_FORMAT, ori_ucm_file, ucm_file));
            System.Threading.Thread.Sleep(100);
        }

        CrasRestart();
        return true;
    }

    
    private void BackupConfig(string folder, string prefix, string ori_prefix, string file_extension)
    {
        string cmd = string.Format("ls {0}ori*", folder);
        if (ExecCmd(cmd) != string.Empty) return;
        //string rename = string.Format("rename {0}{1} {0}{2} *.{3}", folder, prefix, ori_prefix, file_extension);
        ExecCmd(string.Format(RENAME_FORMAT, folder, prefix, ori_prefix, file_extension));
    }


    public void UpdateEffect( ArrayList effects )
    {
        if (!SClient.IsConnected ) return;
        string hifi = UcmPath + "HiFi.conf";
        string result = ExecCmd("grep CODEC -m 1 " + hifi + " | cut -d / -f 4").Trim();
        string wavesFolder =  String.Format("/opt/waves/{0}/", result);

        // BackupConfig(wavesFolder, "waves_params", "ori.waves_params", "*.bin");
        BackupConfig(wavesFolder, "waves_params", "ori.waves_params", "bin");
        ScpClient scpClient = new ScpClient(SClient.ConnectionInfo);
        scpClient.Connect();
        for (int i = 0; i < effects.Count; i++)
        {
            FileInfo file = new FileInfo(effects[i].ToString());
            StringBuilder sb = new StringBuilder(wavesFolder);
            if (effects[i].ToString().EndsWith("headphone_bypass.bin"))
            {
                sb.Append(EFFECT_HEADPHONE_BYPASS);
            }
            else if (effects[i].ToString().EndsWith("speaker_bypass.bin"))
            {
                sb.Append(EFFECT_SPEAKER_BYPASS);
            }
            else if (effects[i].ToString().EndsWith("speaker.bin"))
            {
                sb.Append(EFFECT_SPEAKER);
            }
            else if (effects[i].ToString().EndsWith("headphone.bin"))
            {
                sb.Append(EFFECT_HEADPHONE);
            }
            scpClient.Upload(file, sb.ToString());
        }
        scpClient.Disconnect();
        scpClient.Dispose();
        CrasRestart();
    }

 

    public void UpdateVolumeTable(string filePath)
    {
        Console.WriteLine("UpdateVolumeTable to " + CrasPath);
        var result = ExecCmd(("ls /ect/cras/" + CrasPath + "/" + FILE_ORI_VOLUME_TABLE_NAME).ToString());
        if (result.Contains("No such file or directory") || result == string.Empty)
        {

            StringBuilder sb = new StringBuilder();
            sb.Append("cp ");
            sb.Append(PATH_ETC_CRAS);
            sb.Append(CrasPath);
            sb.Append("/");
            sb.Append(FILE_VOLUME_TABLE_NAME);
            sb.Append(" ");
            sb.Append(PATH_ETC_CRAS);
            sb.Append(CrasPath);
            sb.Append("/");
            sb.Append(FILE_ORI_VOLUME_TABLE_NAME);
            ExecCmd(sb.ToString());
        }
        ScpClient scpClient = new ScpClient(SClient.ConnectionInfo);
        scpClient.Connect();
        if (scpClient.IsConnected)
        {
            try
            {
                FileInfo file = new FileInfo(filePath);
                scpClient.Upload(file, PATH_ETC_CRAS + CrasPath + "/sof-rt5682.card_settings");
                System.Threading.Thread.Sleep(100);
                CrasRestart();
            }
            catch (ScpException exception)
            {
                Console.WriteLine(exception.Message);
            }
        }
        scpClient.Disconnect();
        scpClient.Dispose();

    }

    private void CrasRestart()
    {
        SClient.RunCommand(CMD_OS_SYNC);
        System.Threading.Thread.Sleep(500);
        SClient.RunCommand(CMD_CRAS_START);
        System.Threading.Thread.Sleep(500);
    }


    private void GetSystemInfomation()
    {
        _crasPath           = ExecCmd(CMD_GET_CRAS_CONFIG);
        _ucmSuffix          = ExecCmd(CMD_GET_UCM_SUFFIX);
        _cbProjectCode      = ExecCmd(CMD_GET_PROJECT_CODE);
        _cbOSVersion        = ExecCmd(CMD_GET_OS_VERSION);
        _audioSoundCard     = ExecCmd(CMD_GET_SOUND_CARD).TrimEnd();
        _effectStatus       = IsEffectOn(UcmPath + FILE_HIFI_CONF).ToString();
    }
   

    private string ExecCmd(string cmd)
    {
        Console.WriteLine(cmd);
        var output = SClient.RunCommand(cmd);
        Console.WriteLine(output.Result.ToString());
        return output.Result;
    }

    private void KeyboardAuthentication_AuthenticationPrompt(object sender, Renci.SshNet.Common.AuthenticationPromptEventArgs e)
        {
            foreach (AuthenticationPrompt prompt in e.Prompts)
            {
                if (prompt.Request.StartsWith("Password"))
                    prompt.Response = this.TestPassword;           // Hard coded here just for the sample
                else if (prompt.Request.StartsWith("Verification"))
                    prompt.Response = "...";                // Insert Verification code at runtime using here a Debugger breakpoint
            }
        }


}
