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
    /// �Q�l
    /// http://ftp1.digi.com/support/documentation/90002124_H.pdf
    /// </summary>
    public class XBeeWifi
    {
        #region enum
        /// <summary>
        /// XBee Wifi�X�e�[�^�X
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
        /// �Í�����
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

        #region �萔
        public const int CR = '\r';
        public const int LF = '\n';

        private const int READ_BUFFER_SIZE = 100;

        // Wifi�ڑ��ݒ�
        /* Local Debug
        private const string SSID = "MZ400_1";
        private const string PASSWD = "1223334444";
         */
        private const string SSID = "Buffalo-G-87E0";
        private const string PASSWD = "3hytvyhxtt4rv";
        private const string DST_ADDR = "207.46.134.203";

        // �T�[�r�X����HOST�s
        private const string HOST_LINE = " HTTP/1.1\r\nHOST: aroma-black.cloudapp.net\r\n";
        // �d���擾�T�[�r�XURL
        private const string GET_WEIGHT_URL = "GET /WeightRestWCFService.svc/latestweight" + HOST_LINE;
        // �d���o�^�T�[�r�XURL
        private const string SET_WEIGHT_URL = "GET /WeightRestWCFService.svc/addweight/";
        #endregion

        #region private�ϐ�
        /// <summary>
        /// �ǂݍ��݃o�b�t�@
        /// </summary>
        private byte[] m_readBuffer = new byte[READ_BUFFER_SIZE];

        /// <summary>
        /// XBee Wifi���W���[��
        /// </summary>
        private XBee m_xBee;
        #endregion

        #region �R���X�g���N�^
        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="xBee">XBee Wifi���W���[��</param>
        public XBeeWifi(XBee xBee)
        {
            this.m_xBee = xBee;
            //Xbee Config Setup X-XCTU�Őݒ肵�Ă����̂ŕs�v
            //m_xBee.Configure(115200, GT.Interfaces.Serial.SerialParity.None, GT.Interfaces.Serial.SerialStopBits.One, 8);
        }
        #endregion

        #region AT�R�}���h���M
        /// <summary>
        /// AT�R�}���h���[�h�ɓ���܂��B
        /// </summary>
        /// <returns>���������ꍇ��true</returns>
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
        /// �X�e�[�^�X���擾���܂��B
        /// ���s�O��AT�R�}���h���[�h�ɓ����Ă���K�v������܂��B
        /// </summary>
        /// <returns>�X�e�[�^�X</returns>
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
        /// Wifi�ڑ���ݒ肷��B
        /// </summary>
        public void SetUp()
        {
            // �ڑ��ݒ�
            SetUpInfrastructureMode(SSID, XBeeWifi.Encryption.WPA2, PASSWD);

            // �T�[�r�X�ڑ���ݒ�
            SetDestination(DST_ADDR, 80);

            //��M�|�[�g���w��
            //SetRecivePort(80);
        }

        /// <summary>
        /// �C���t���X�g���N�`�����[�h��Wifi�R�l�N�V�������\�z���܂��B
        /// ���s�O��AT�R�}���h���[�h�ɓ����Ă���K�v������܂��B
        /// </summary>
        /// <param name="ssid">SSID</param>
        /// <param name="enc">�Í�����</param>
        /// <param name="password">�p�X���[�h</param>
        private void SetUpInfrastructureMode(string ssid, Encryption enc, string password)
        {
            int encInt = (int)enc;

            Send("ATAH 2"); // 0:Ad-hoc Joiner, 1:Ad-Hoc Creator, 2:�C���t���X�g���N�`�����[�h
            //Send("ATAH 0");
            Send("ATID " + ssid); // SSID
            Send("ATEE " + encInt.ToString("X")); // 0:open / 1:WPA PSK / 2:WPA2 PSK / 3:WEP 40 / 4:WEP 104
            Send("ATPK " + password); // Password
            Send("ATMA 0"); // 0:DHCP / 1:Static
            Send("ATIP 1"); // 0:UDP 1:TCP
        }

        /// <summary>
        /// ������IP�A�h���X�A�|�[�g�ԍ���ݒ肵�܂��B
        /// ���s�O��AT�R�}���h���[�h�ɓ����Ă���K�v������܂��B
        /// </summary>
        /// <param name="ip">IP�A�h���X</param>
        /// <param name="port">�|�[�g�ԍ�</param>
        private void SetDestination(string ip, int port)
        {
            Send("ATDL " + ip); //����IP�A�h���X
            Send("ATDE " + port.ToString("X")); //����IP�A�h���X
        }

        /// <summary>
        /// Xbee���̃|�[�g�ԍ��i��M�|�[�g�j��ύX���܂��B
        /// ���s�O��AT�R�}���h���[�h�ɓ����Ă���K�v������܂��B
        /// </summary>
        /// <param name="port">�|�[�g�ԍ�</param>
        private void SetRecivePort(int port)
        {
            Send("ATD0 " + port.ToString("X")); //�����̃|�[�g
        }

        /// <summary>
        /// ���蓖�Ă�ꂽIP�A�h���X��Ԃ��܂��B
        /// ���s�O��AT�R�}���h���[�h�ɓ����Ă���K�v������܂��B
        /// <para>ATMY�R�}���h</para>
        /// </summary>
        public string GetMyIpAddress()
        {
            return Send("ATMY");
        }

        /// <summary>
        /// AT�R�}���h���[�h���甲���܂��B
        /// �|�[�g�͊J�����܂܂ł��B
        /// </summary>
        public void DropOutOfCommandMode()
        {
            this.OpenIfClosed();
            Send("ATCN");
        }

        /// <summary>
        /// Wifi�ڑ������Z�b�g����B
        /// </summary>
        public void Reset()
        {
            if (EnterAtCommandMode())
            {
                Send("ATFR");
            }
        }
        #endregion

        #region �V���A���|�[�g����M
        /// <summary>
        /// �V���A���|�[�g�R�l�N�V�������J���܂��B
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
        /// AT�R�}���h�𑗂�܂��B
        /// ���O��EnterAtCommandMode()���\�b�h�����s���AAT�R�}���h���[�h�ɓ����Ă���K�v������܂��B
        /// AT�R�}���h���[�h���I������ꍇ��DropOutOfCommandMode()���\�b�h�����s���Ă��������B
        /// </summary>
        /// <param name="data">�R�}���h������</param>
        /// <returns>���X�|���X</returns>
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
        /// �R�}���h�𑗂�܂��B
        /// ���O��EnterAtCommandMode()���\�b�h�����s���AAT�R�}���h���[�h�ɓ����Ă���K�v������܂��B
        /// </summary>
        /// <param name="data">�R�}���h������</param>
        /// <returns>���X�|���X</returns>
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
        /// �V���A���|�[�g����P�s�ǂݍ��݂܂��B
        /// </summary>
        /// <param name="newLine">���s����</param>
        /// <returns>�ǂݍ��񂾂P�s</returns>
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

        #region HTTP����M
        /// <summary>
        /// HTTP�w�b�_���擾���AContentLength��Ԃ��B
        /// </summary>
        /// <returns>ContentLength</returns>
        private int GetContentLength()
        {
            // HTTP �X�e�[�^�X�ǂݍ���
            string line = ReadLine(XBeeWifi.LF);
            string[] cols = line.Split();
            if (cols.Length < 2 || cols[0] != "HTTP/1.1")
                throw new Exception("Response ERROR: " + line);

            if (Convert.ToInt32(cols[1]) != 200)
                throw new Exception("HTTP ERROR: " + cols[1]);

            // Content-Length �擾
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
        /// �o�b�t�@�ɓ��e��ǂݍ��ށB
        /// </summary>
        /// <param name="buf">�o�b�t�@</param>
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
        /// �d�����擾����B
        /// </summary>
        /// <returns>�d��</returns>
        public string GetWeight()
        {
            // HTTP GET���M
            Send(GET_WEIGHT_URL, true, false);

            // HTTP�w�b�_�擾
            int contLen = GetContentLength();

            // �o�b�t�@�ɓ��e��ǂݍ���
            byte[] buf = new byte[contLen];
            ReadBuf(buf);

            // ���ʎ擾
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
        /// �d����o�^����B
        /// </summary>
        /// <param name="weight">�d��</param>
        /// <returns>True:����, False:���s</returns>
        public void SetWeight(int weight)
        {
            // HTTP GET���M
            Send(SET_WEIGHT_URL + weight + HOST_LINE, true, false);

            // HTTP�w�b�_�擾
            int contLen = GetContentLength();

            // �o�b�t�@�ɓ��e��ǂݍ���
            byte[] buf = new byte[contLen];
            ReadBuf(buf);
        }
        #endregion
    }
}
