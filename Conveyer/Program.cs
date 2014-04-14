using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace GadgeteerDemo
{
    /// <summary>
    /// 搬送機プログラム
    /// </summary>
    public partial class Program
    {
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");

            // Wifi接続
            XBeeWifi wifi = new XBeeWifi(xBee);
            wifi.EnterAtCommandMode();
            wifi.SetUp();

            // Wifi接続待ち
            DateTime t = DateTime.Now;
            while (true)
            {
                XBeeWifi.Status status = wifi.GetStatus();

                if (status == XBeeWifi.Status.Success)
                {
                    Debug.Print("Connection Success!!");
                    break;
                }
                else
                {
                    Debug.Print(status.ToString());
                }

                Thread.Sleep(3000);

                if ((DateTime.Now - t) > new TimeSpan(0, 0, 60))
                {
                    Debug.Print("WifiConnectFail.");
                    return;
                }
            }

            // IPアドレス確認
            var myAddr = wifi.GetMyIpAddress();
            Debug.Print("XbeeIP:" + myAddr);
            wifi.DropOutOfCommandMode();

            int errCnt = 0;

            // タイマー設定
            GT.Timer timer = new GT.Timer(5000);
            timer.Tick += (tick) =>
            {
                try
                {
                    // 重さ取得
                    string result = wifi.GetWeight();
                    Debug.Print(result);

                    int startIdx = result.IndexOf("@");
                    int endIdx = result.LastIndexOf("@");
                    if (startIdx + 1 < endIdx)
                    {
                        int weight = int.Parse(result.Substring(startIdx + 1, endIdx - startIdx - 1));
                        if (weight < -5)
                        {
                            // モーターを１回転する
                            motorControllerL298.MoveMotor(MotorControllerL298.Motor.Motor1, -58);
                            Thread.Sleep(2000);
                            motorControllerL298.MoveMotor(MotorControllerL298.Motor.Motor1, 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errCnt++;
                    // エラー表示
                    Debug.Print(errCnt + " " + ex.Message);
                }

                if (errCnt == 5)
                {
                    timer.Stop();
                    wifi.Reset();
                    Reboot();
                }
            };

            // タイマー開始
            timer.Start();
        }
    }
}
