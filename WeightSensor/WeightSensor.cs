using System;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;
using Gadgeteer.Modules.Seeed;
using System.Threading;
using Gadgeteer.Interfaces;

using GT = Gadgeteer;
using System.Collections;

namespace GadgeteerDemo
{
    /// <summary>
    /// �d���Z���T�[
    /// </summary>
    public class WeightSensor
    {
        #region �萔
        /// <summary>
        /// �����T�C�Y
        /// </summary>
        //const int HistorySize = 80;

        /// <summary>
        /// �T���v�����O��
        /// </summary>
        const int NSampling = 50;

        /// <summary>
        /// �P�̏d��
        /// </summary>
        const short WeightPerQuantity = 10;
        #endregion

        #region private�ϐ�
        /// <summary>
        /// Breakout���W���[��
        /// </summary>
        private Breakout_TB10 m_breakout_TB10;

        /// <summary>
        /// �d��
        /// </summary>
        private short m_weight = 0;

        /// <summary>
        /// �d���̗���
        /// </summary>
        //private ShortRingBuffer m_weightHistory = new ShortRingBuffer(HistorySize);

        /// <summary>
        /// ���܂̏d��
        /// </summary>
        private short m_tareWeight = 0;

        /// <summary>
        /// �d���Z���T�[����
        /// </summary>
        private AnalogInput m_weightSensorInput;
        #endregion

        #region �R���X�g���N�^
        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="breakout_TB10">Breakout���W���[��</param>
        public WeightSensor(Breakout_TB10 breakout_TB10)
        {
            this.m_breakout_TB10 = breakout_TB10;

            m_weightSensorInput = m_breakout_TB10.SetupAnalogInput(Gadgeteer.Socket.Pin.Five);
        }
        #endregion

        /// <summary>
        /// �d�������[�h���܂��B
        /// reset��true�̏ꍇ�͕��܂��擾���܂��B
        /// </summary>
        /// <param name="reset">���Z�b�g���ǂ���</param>
        public void LoadWeight(bool reset = false)
        {
            short[] array1 = new short[NSampling];
            short[] array2 = new short[NSampling];
            int n1 = NSampling;
            int n2;

            for (int i = 0; i < NSampling; ++i)
            {
                array1[i] = ConvertProportionToWeight(m_weightSensorInput.ReadProportion());
                Thread.Sleep(500/NSampling);
            }

            // �����l�t�B���^
            DigitalFilterMedian(array1, n1, array2, out n2);

            // ���ϒl�t�B���^
            DigitalFilterAverage(array2, n2, array1, out n1);

            // ���ς��Ƃ�
            int sum = 0;
            for (int i = 0; i < n1; ++i)
            {
                sum += array1[i];
            }
            short avg = (short)(System.Math.Round(sum / n1));

            if (reset)
            {
                // ���߂��d���𕗑܂Ƃ���B
                m_tareWeight = avg;
            }
            else
            {
                // ���܂������āA�d�������߂�B
                m_weight = (short)(avg - m_tareWeight);

                // �ߋ��̃f�[�^�����ɕ␳����
                //m_weightHistory.Add(m_weight);
            }
        }

        /// <summary>
        /// �f�[�^�Ƀt�B���^�������܂��B
        /// �R�������l���Ƃ��ĕ��������܂��B
        /// </summary>
        /// <param name="arrayIn">���̓f�[�^</param>
        /// <param name="nIn">���̓f�[�^��</param>
        /// <param name="arrayOut">�o�̓f�[�^</param>
        /// <param name="nOut">�o�̓f�[�^��</param>
        private void DigitalFilterMedian(short[] arrayIn, int nIn, short[] arrayOut, out int nOut)
        {
            if (nIn - 2 > 0)
            {
                // �R���A�����l������Ă���
                nOut = nIn - 2;
                for (int i = 0; i < nOut; ++i)
                {
                    int a1 = arrayIn[i];
                    int a2 = arrayIn[i + 1];
                    int a3 = arrayIn[i + 2];

                    // a1 �� a2 �� a3 �ƂȂ�悤�Ƀ\�[�g����
                    {
                        int tmp;
                        if (a1 > a3)
                        {
                            tmp = a3;
                            a3 = a1;
                            a1 = tmp;
                        }

                        if (a1 > a2)
                        {
                            tmp = a2;
                            a2 = a1;
                            a1 = tmp;
                        }

                        if (a2 > a3)
                        {
                            tmp = a3;
                            a3 = a2;
                            a2 = tmp;
                        }
                    }

                    arrayOut[i] = (short)a2;
                }
            }
            else
            {
                nOut = nIn;
                Array.Copy(arrayIn, arrayOut, nIn);
            }
        }


        /// <summary>
        /// �f�[�^�Ƀt�B���^�������܂��B
        /// �R�����ϒl���Ƃ��ĕ��������܂��B
        /// </summary>
        /// <param name="arrayIn">���̓f�[�^</param>
        /// <param name="nIn">���̓f�[�^��</param>
        /// <param name="arrayOut">�o�̓f�[�^</param>
        /// <param name="nOut">�o�̓f�[�^��</param>
        private void DigitalFilterAverage(short[] arrayIn, int nIn, short[] arrayOut, out int nOut)
        {
            if (nIn - 2 > 0)
            {
                // �R���A���ϒl������Ă���
                nOut = nIn - 2;
                for (int i = 0; i < nOut; ++i)
                {
                    int sum = arrayIn[i] + arrayIn[i + 1] + arrayIn[i + 2];
                    arrayOut[i] = (short)System.Math.Round(sum / 3.0);
                }
            }
            else
            {
                nOut = nIn;
                Array.Copy(arrayIn, arrayOut, nIn);
            }
        }

        /// <summary>
        /// �d�����擾���܂��B
        /// </summary>
        /// <returns>�d��[g]</returns>
        public short GetWeight()
        {
            return m_weight;
        }

#if History
        /// <summary>
        /// �d���������擾���܂��B
        /// </summary>
        /// <returns>�d���̗���</returns>
        public short[] GetWeightHistory()
        {
            short[] history = new short[m_weightHistory.Count];
            lock (m_weightHistory)
            {
                int i = 0;
                foreach (short value in m_weightHistory)
                {
                    history[i++] = value;
                }
            }
            return history;
        }

        /// <summary>
        /// �d�������̍ő吔���擾���܂��B
        /// </summary>
        /// <returns>�d�������̍ő吔</returns>
        public int GetWeightHistoryMaxSize()
        {
            return HistorySize;
        }
#endif
        /// <summary>
        /// �����擾���܂��B
        /// </summary>
        /// <returns>��</returns>
        public short GetQuantity()
        {
            return (short) System.Math.Round((double)m_weight / WeightPerQuantity);
        }

        /// <summary>
        /// ���݂̏d����0g�Ƃ��܂��B
        /// </summary>
        public void ResetWeight()
        {
            LoadWeight(true);
        }


        /// <summary>
        /// 0�`1�̓��͒l���O�����P�ʂɕϊ����܂��B
        /// </summary>
        /// <param name="proportion">0�`1�̃A�i���O���͒l</param>
        /// <returns>�d��[g]</returns>
        private short ConvertProportionToWeight(double proportion)
        {
            const double K = 2060;// 2048; //�W��(�����l���狁�߂�)

            return (short)(proportion * K);
        }

#if History
        #region ShortRingBuffer
        /// <summary>
        /// �����O�o�b�t�@
        /// </summary>
        class ShortRingBuffer : IEnumerable
        {
            private short[] m_data;

            private int m_startIndex;

            private int m_endIndex;

            private bool m_isFull;

            /// <summary>
            /// �R���X�g���N�^
            /// </summary>
            /// <param name="size">�T�C�Y</param>
            public ShortRingBuffer(int size)
            {
                m_data = new short[size];
                m_startIndex = 0;
                m_endIndex = -1;
                m_isFull = false;
            }

            /// <summary>
            /// ���e���N���A���܂��B
            /// </summary>
            public void Clear()
            {
                m_startIndex = 0;
                m_endIndex = -1;
                m_isFull = false;
            }

            /// <summary>
            /// �f�[�^�������ς����ǂ���
            /// </summary>
            public bool IsFull
            {
                get { return m_isFull; }
            }

            /// <summary>
            /// �����O�o�b�t�@�Ƀf�[�^���P�ǉ����܂��B
            /// </summary>
            /// <param name="data">�f�[�^</param>
            public void Add(short data)
            {
                if (m_endIndex + 1 == m_data.Length)
                {
                    m_data[0] = data;
                    m_endIndex = 0;
                    m_startIndex = 1;
                    m_isFull = true;
                }
                else if (m_isFull)
                {
                    if (m_startIndex + 1 == m_data.Length)
                    {
                        m_startIndex = 0;
                    }
                    else
                    {
                        m_startIndex++;
                    }

                    m_endIndex++;
                    m_data[m_endIndex] = data;

                }
                else
                {
                    m_endIndex++;
                    m_data[m_endIndex] = data;
                }
            }

            /// <summary>
            /// �����q���擾���܂��B
            /// </summary>
            /// <returns>�����q</returns>
            public IEnumerator GetEnumerator()
            {
                if (m_startIndex == 0)
                {
                    for (int i = m_startIndex; i <= m_endIndex; ++i)
                    {
                        yield return m_data[i];
                    }
                }
                else
                {
                    for (int i = m_startIndex; i < m_data.Length; ++i)
                    {
                        yield return m_data[i];
                    }
                    for (int i = 0; i <= m_endIndex; ++i)
                    {
                        yield return m_data[i];
                    }
                }
            }

            /// <summary>
            /// �f�[�^�̌���Ԃ��܂��B
            /// </summary>
            public int Count
            {
                get { return (m_isFull) ? m_data.Length : m_endIndex + 1; }
            }
        }
        #endregion
#endif
    }
}
