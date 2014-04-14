using System;
using System.Threading;

using Microsoft.SPOT;

using GT = Gadgeteer;
using Gadgeteer.Modules.GHIElectronics;
using Gadgeteer.Modules.Seeed;

namespace GadgeteerDemo
{
    /// <summary>
    /// 重さ取得・登録プログラム
    /// </summary>
    public partial class Program
    {
        /// <summary>
        /// Default Font
        /// </summary>
        private static readonly Font DEFAULT_FONT = Resources.GetFont(Resources.FontResources.small);

        /// <summary>
        /// This method is run when the mainboard is powered up or reset.   
        /// </summary>
        private void ProgramStarted()
        {
            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");

            //Debug.Print("Free Memmory: " + Debug.GC(true));

            //Print Display
            ShowMessage("Started.", 5, 5);

            // 温度取得イベントハンドラ設定
            temperatureHumidity.MeasurementComplete += (sender, temperature, relativeHumidity) =>
                {
                    string ts = (temperature + 0.05).ToString();
                    int idx = ts.IndexOf(".");
                    string tf = idx > 0 && idx + 1 < ts.Length ? ts.Substring(0, idx + 2) : ts;
                    ShowMessage("Temperature: " + tf, 5, 80);
                };

            // 温度取得開始
            temperatureHumidity.StartContinuousMeasurements();

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
                    ShowMessage("WifiConnectFail.",5, 5);
                    return;
                }
            }

            // IPアドレス確認
            var myAddr = wifi.GetMyIpAddress();
            ShowMessage("XbeeIP:" + myAddr, 5, 20);
            wifi.DropOutOfCommandMode();

            // 重量センサークラス作成
            WeightSensor model = new WeightSensor(breakout_TB10);

            // 電圧が安定するまで待機
            Thread.Sleep(50000);

            // 風袋をリセット
            model.ResetWeight();

            try
            {
                // 初期登録
                wifi.SetWeight(0);
            }
            catch (Exception ex)
            {
                // エラー表示
                ShowMessage("0 " + ex.Message, 5, 5, true);
            }
            
            bool error = false;
            int errCnt = 0;
            int preQuantity = 0;

            // タイマー設定
            GT.Timer timer = new GT.Timer(1000);
            timer.Tick += (tick) =>
            {
                // 重さ検知
                model.LoadWeight();
                ShowMessage("Weight: " + model.GetWeight() + "G", 5, 35);

                // 個数取得
                int curQuantity = model.GetQuantity();
                ShowMessage("Quantity: " + curQuantity, 5, 50);

                try
                {
                    // 個数が変わったときに重さ登録
                    if (preQuantity != curQuantity)
                    {
                        wifi.SetWeight(model.GetWeight());
                        preQuantity = curQuantity;
                    }

                    // 重さ取得
                    //string result = wifi.GetWeight();
                    //ShowMessage(result, 5, 65);

                    // エラークリア
                    if (error)
                    {
                        ShowMessage("", 5, 5, true);
                        error = false;
                    }
                }
                catch (Exception ex)
                {
                    errCnt++;
                    // エラー表示
                    ShowMessage(errCnt + " " + ex.Message, 5, 5, true);
                    error = true;
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

        /// <summary>
        /// ディスプレイにメッセージを表示する。
        /// </summary>
        /// <param name="text">メッセージ</param>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="red">赤字フラグ</param>
        private void ShowMessage(string text, uint x = 0, uint y = 0, bool red = false)
        {
            int width;
            int height;
            DEFAULT_FONT.ComputeExtent(text, out width, out height);

            GT.Color color = red ? GT.Color.Red : GT.Color.Yellow;

            using (Bitmap bmp = new Bitmap(119, height))
            {
                bmp.DrawText(text, DEFAULT_FONT, color, 0, 0);
                display_N18.Draw(bmp, x, y);
            }

            if (text.Length > 0) Debug.Print("[Message] " + text);
        }
    }
}
