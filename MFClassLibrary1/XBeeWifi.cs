using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

using Microsoft.SPOT;

using GT = Gadgeteer;
using Gadgeteer.Modules.GHIElectronics;

namespace GadgeteerDemo
{
    /// <summary>
    /// XBee Wifi
    /// 
    /// 参考
    /// http://ftp1.digi.com/support/documentation/90002124_H.pdf
    /// </summary>
    public class XBeeWifi
    {
        #region enum
        /// <summary>
        /// XBee Wifiステータス
        /// </summary>
        public enum Status : int
        {
            /// <summary>
            /// Successfully joined an access point, established IP addresses and IP listening sockets.
            /// </summary>
            Success = 0x00,

            /// <summary>
            /// WiFi transceiver initialization in progress.
            /// </summary>
            Initialization = 0x01,

            /// <summary>
            /// WiFi transceiver initialized, but not yet scanning for access point.
            /// </summary>
            ScanningForAccessPoint = 0x02,

            /// <summary>
            /// Disconnecting from access point
            /// </summary>
            DisconnectingFromAccessPoint = 0x13,

            /// <summary>
            /// SSID not configured
            /// </summary>
            SSIDNotConfigured = 0x23,

            /// <summary>
            /// SSID was found, but join failed
            /// </summary>
            JoinFailed = 0x27,

            /// <summary>
            /// Module joined a network and is waiting for IP configuration to complete, which usually means it is waiting for a DHCP provided address.
            /// </summary>
            WaitingForDHCP = 0x41,

            /// <summary>
            /// Module is joined, IP is configured, and listening sockets are being set up
            /// </summary>
            ListeningSocketSetUp = 0x42,

            /// <summary>
            /// Module is currently scanning for the configured SSID
            /// </summary>
            ScanningForSSID = 0xFF
        }

        /// <summary>
        /// 暗号方式
        /// </summary>
        public enum Encryption : int
        {
            /// <summary>
            /// No security
            /// </summary>
            None = 0,

            /// <summary>
            /// WPA
            /// </summary>
            WPA = 1,

            /// <summary>
            /// WPA2
            /// </summary>
            WPA2 = 2,

            /// <summary>
            /// WEP
            /// </summary>
            WEP = 3,
        }
        #endregion

        #region 定数
        public const int CR = '\r';
        public const int LF = '\n';

        private const int READ_BUFFER_SIZE = 100;

        // Wifi接続設定
        /* Local Debug
        private const string SSID = "MZ400_1";
        private const string PASSWD = "1223334444";
         */
        private const string SSID = "Buffalo-G-87E0";
        private const string PASSWD = "3hytvyhxtt4rv";
        private const string DST_ADDR = "207.46.134.203";

        // サービス共通HOST行
        private const string HOST_LINE = " HTTP/1.1\r\nHOST: aroma-black.cloudapp.net\r\n";
        // 重さ取得サービスURL
        private const string GET_WEIGHT_URL = "GET /WeightRestWCFService.svc/latestweight" + HOST_LINE;
        // 重さ登録サービスURL
        private const string SET_WEIGHT_URL = "GET /WeightRestWCFService.svc/addweight/";
        #endregion

        #region private変数
        /// <summary>
        /// 読み込みバッファ
        /// </summary>
        private byte[] m_readBuffer = new byte[READ_BUFFER_SIZE];

        /// <summary>
        /// XBee Wifiモジュール
        /// </summary>
        private XBee m_xBee;
        #endregion

        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="xBee">XBee Wifiモジュール</param>
        public XBeeWifi(XBee xBee)
        {
            this.m_xBee = xBee;
            //Xbee Config Setup X-XCTUで設定しておくので不要
            //m_xBee.Configure(115200, GT.Interfaces.Serial.SerialParity.None, GT.Interfaces.Serial.SerialStopBits.One, 8);
        }
        #endregion

        #region ATコマンド送信
        /// <summary>
        /// ATコマンドモードに入ります。
        /// </summary>
        /// <returns>成功した場合はtrue</returns>
        public bool EnterAtCommandMode()
        {
            this.OpenIfClosed();

            m_xBee.SerialLine.Write("+++");
            m_xBee.SerialLine.Flush();

            string response = ReadLine();

            if (response == "OK")
            {
                Debug.Print("Success to enter AT command mode. Response is '" + response + "'");
                return true;
            }
            else
            {
                Debug.Print("Failed to enter AT command mode. Response is '" + response + "'");
                return false;
            }
        }

        /// <summary>
        /// ステータスを取得します。
        /// 実行前にATコマンドモードに入っている必要があります。
        /// </summary>
        /// <returns>ステータス</returns>
        public Status GetStatus()
        {
            string response = Send("ATAI");
            if (response.Equals("FF"))
                return Status.ScanningForSSID;
            if (response.Length == 0)
                return Status.Initialization;
            int responseInt = int.Parse(response);

            return (Status)responseInt;
        }

        /// <summary>
        /// Wifi接続を設定する。
        /// </summary>
        public void SetUp()
        {
            // 接続設定
            SetUpInfrastructureMode(SSID, XBeeWifi.Encryption.WPA2, PASSWD);

            // サービス接続先設定
            SetDestination(DST_ADDR, 80);

            //受信ポートを指定
            //SetRecivePort(80);
        }

        /// <summary>
        /// インフラストラクチャモードでWifiコネクションを構築します。
        /// 実行前にATコマンドモードに入っている必要があります。
        /// </summary>
        /// <param name="ssid">SSID</param>
        /// <param name="enc">暗号方式</param>
        /// <param name="password">パスワード</param>
        private void SetUpInfrastructureMode(string ssid, Encryption enc, string password)
        {
            int encInt = (int)enc;

            Send("ATAH 2"); // 0:Ad-hoc Joiner, 1:Ad-Hoc Creator, 2:インフラストラクチャモード
            //Send("ATAH 0");
            Send("ATID " + ssid); // SSID
            Send("ATEE " + encInt.ToString("X")); // 0:open / 1:WPA PSK / 2:WPA2 PSK / 3:WEP 40 / 4:WEP 104
            Send("ATPK " + password); // Password
            Send("ATMA 0"); // 0:DHCP / 1:Static
            Send("ATIP 1"); // 0:UDP 1:TCP
        }

        /// <summary>
        /// 相手先のIPアドレス、ポート番号を設定します。
        /// 実行前にATコマンドモードに入っている必要があります。
        /// </summary>
        /// <param name="ip">IPアドレス</param>
        /// <param name="port">ポート番号</param>
        private void SetDestination(string ip, int port)
        {
            Send("ATDL " + ip); //相手IPアドレス
            Send("ATDE " + port.ToString("X")); //相手IPアドレス
        }

        /// <summary>
        /// Xbee側のポート番号（受信ポート）を変更します。
        /// 実行前にATコマンドモードに入っている必要があります。
        /// </summary>
        /// <param name="port">ポート番号</param>
        private void SetRecivePort(int port)
        {
            Send("ATD0 " + port.ToString("X")); //自分のポート
        }

        /// <summary>
        /// 割り当てられたIPアドレスを返します。
        /// 実行前にATコマンドモードに入っている必要があります。
        /// <para>ATMYコマンド</para>
        /// </summary>
        public string GetMyIpAddress()
        {
            return Send("ATMY");
        }

        /// <summary>
        /// ATコマンドモードから抜けます。
        /// ポートは開いたままです。
        /// </summary>
        public void DropOutOfCommandMode()
        {
            this.OpenIfClosed();
            Send("ATCN");
        }

        /// <summary>
        /// Wifi接続をリセットする。
        /// </summary>
        public void Reset()
        {
            if (EnterAtCommandMode())
            {
                Send("ATFR");
            }
        }
        #endregion

        #region シリアルポート送受信
        /// <summary>
        /// シリアルポートコネクションを開きます。
        /// </summary>
        private void OpenIfClosed()
        {
            if (!m_xBee.SerialLine.IsOpen)
            {
                m_xBee.SerialLine.ReadTimeout = 5000;
                m_xBee.SerialLine.WriteTimeout = 5000;
                m_xBee.SerialLine.Open();
            }
        }

        /// <summary>
        /// ATコマンドを送ります。
        /// 事前にEnterAtCommandMode()メソッドを実行し、ATコマンドモードに入っている必要があります。
        /// ATコマンドモードを終了する場合はDropOutOfCommandMode()メソッドを実行してください。
        /// </summary>
        /// <param name="data">コマンド文字列</param>
        /// <returns>レスポンス</returns>
        public string Send(string data,bool isCrLf = false, bool isResponse = true)
        {
            m_xBee.SerialLine.DiscardInBuffer();
            m_xBee.SerialLine.DiscardOutBuffer();

            m_xBee.SerialLine.Write(data);
            m_xBee.SerialLine.Write((byte)CR);
            if (isCrLf)
            {
                m_xBee.SerialLine.Write((byte)LF);
            }
            m_xBee.SerialLine.Flush();
            Debug.Print("Send:" + data);

            if (isResponse)
            {
                string response = ReadLine();
                Debug.Print("Response:" + response);
                return response;
            }
            return "";
        }

        /// <summary>
        /// コマンドを送ります。
        /// 事前にEnterAtCommandMode()メソッドを実行し、ATコマンドモードに入っている必要があります。
        /// </summary>
        /// <param name="data">コマンド文字列</param>
        /// <returns>レスポンス</returns>
        public string Send(byte[] data, bool isCrLf = false)
        {
            m_xBee.SerialLine.DiscardInBuffer();
            m_xBee.SerialLine.DiscardOutBuffer();

            m_xBee.SerialLine.Write(data, 0, data.Length);
            m_xBee.SerialLine.Write((byte)CR);
            if (isCrLf)
            {
                m_xBee.SerialLine.Write((byte)LF);
            }
            m_xBee.SerialLine.Flush();
            Debug.Print("Send:" + data);

            string response = ReadLine();
            Debug.Print("Response:" + response);

            return response;
        }


        /// <summary>
        /// シリアルポートから１行読み込みます。
        /// </summary>
        /// <param name="newLine">改行文字</param>
        /// <returns>読み込んだ１行</returns>
        public string ReadLine(int newLine = CR)
        {
            string result = string.Empty;
            int bufferCount = 0;

            while (true)
            {
                int c = m_xBee.SerialLine.ReadByte();

                if (c == newLine || c == -1)
                {
                    result += new string(Encoding.UTF8.GetChars(m_readBuffer, 0, bufferCount));

                    return result;
                }
                else if (c != CR)
                {
                    if (bufferCount + 1 >= READ_BUFFER_SIZE)
                    {
                        result += new string(Encoding.UTF8.GetChars(m_readBuffer, 0, bufferCount));
                        bufferCount = 0;
                    }
                    m_readBuffer[bufferCount++] = (byte)c;
                }
            }

        }
        #endregion

        #region HTTP送受信
        /// <summary>
        /// HTTPヘッダを取得し、ContentLengthを返す。
        /// </summary>
        /// <returns>ContentLength</returns>
        private int GetContentLength()
        {
            // HTTP ステータス読み込み
            string line = ReadLine(XBeeWifi.LF);
            string[] cols = line.Split();
            if (cols.Length < 2 || cols[0] != "HTTP/1.1")
                throw new Exception("Response ERROR: " + line);

            if (Convert.ToInt32(cols[1]) != 200)
                throw new Exception("HTTP ERROR: " + cols[1]);

            // Content-Length 取得
            int contLen = 0;
            while ((line = ReadLine(XBeeWifi.LF)) != string.Empty)
            {
                cols = line.Split();
                if (cols.Length >= 2 && cols[0] == "Content-Length:")
                    contLen = Convert.ToInt32(cols[1]);
            }

            if (contLen == 0) throw new Exception("ContentLength: 0");

            return contLen;
        }

        /// <summary>
        /// バッファに内容を読み込む。
        /// </summary>
        /// <param name="buf">バッファ</param>
        private void ReadBuf(byte[] buf)
        {
            int contLen = buf.Length;
            int pos = 0;
            while (pos < contLen)
                pos += m_xBee.SerialLine.Read(buf, pos, contLen - pos);

            string response = new string(Encoding.UTF8.GetChars(buf, 0, pos));
            Debug.Print(response);

            if (pos != contLen) throw new Exception("Read ERROR: " + pos + "/" + contLen);
        }

        /// <summary>
        /// 重さを取得する。
        /// </summary>
        /// <returns>重さ</returns>
        public string GetWeight()
        {
            // HTTP GET送信
            Send(GET_WEIGHT_URL, true, false);

            // HTTPヘッダ取得
            int contLen = GetContentLength();

            // バッファに内容を読み込み
            byte[] buf = new byte[contLen];
            ReadBuf(buf);

            // 結果取得
            string result = null;

            using (MemoryStream ms = new MemoryStream(buf))
            using (XmlReader reader = XmlReader.Create(ms))
            {
                reader.Read();
                result = reader.Name + ": " + reader.ReadElementString();
            }

            return result;
        }

        /// <summary>
        /// 重さを登録する。
        /// </summary>
        /// <param name="weight">重さ</param>
        /// <returns>True:成功, False:失敗</returns>
        public void SetWeight(int weight)
        {
            // HTTP GET送信
            Send(SET_WEIGHT_URL + weight + HOST_LINE, true, false);

            // HTTPヘッダ取得
            int contLen = GetContentLength();

            // バッファに内容を読み込み
            byte[] buf = new byte[contLen];
            ReadBuf(buf);
        }
        #endregion
    }
}
