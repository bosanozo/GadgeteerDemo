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
    /// 重さセンサー
    /// </summary>
    public class WeightSensor
    {
        #region 定数
        /// <summary>
        /// 履歴サイズ
        /// </summary>
        //const int HistorySize = 80;

        /// <summary>
        /// サンプリング回数
        /// </summary>
        const int NSampling = 50;

        /// <summary>
        /// １個の重さ
        /// </summary>
        const short WeightPerQuantity = 10;
        #endregion

        #region private変数
        /// <summary>
        /// Breakoutモジュール
        /// </summary>
        private Breakout_TB10 m_breakout_TB10;

        /// <summary>
        /// 重さ
        /// </summary>
        private short m_weight = 0;

        /// <summary>
        /// 重さの履歴
        /// </summary>
        //private ShortRingBuffer m_weightHistory = new ShortRingBuffer(HistorySize);

        /// <summary>
        /// 風袋の重さ
        /// </summary>
        private short m_tareWeight = 0;

        /// <summary>
        /// 重さセンサー入力
        /// </summary>
        private AnalogInput m_weightSensorInput;
        #endregion

        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="breakout_TB10">Breakoutモジュール</param>
        public WeightSensor(Breakout_TB10 breakout_TB10)
        {
            this.m_breakout_TB10 = breakout_TB10;

            m_weightSensorInput = m_breakout_TB10.SetupAnalogInput(Gadgeteer.Socket.Pin.Five);
        }
        #endregion

        /// <summary>
        /// 重さをロードします。
        /// resetがtrueの場合は風袋を取得します。
        /// </summary>
        /// <param name="reset">リセットかどうか</param>
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

            // 中央値フィルタ
            DigitalFilterMedian(array1, n1, array2, out n2);

            // 平均値フィルタ
            DigitalFilterAverage(array2, n2, array1, out n1);

            // 平均をとる
            int sum = 0;
            for (int i = 0; i < n1; ++i)
            {
                sum += array1[i];
            }
            short avg = (short)(System.Math.Round(sum / n1));

            if (reset)
            {
                // 求めた重さを風袋とする。
                m_tareWeight = avg;
            }
            else
            {
                // 風袋を引いて、重さを求める。
                m_weight = (short)(avg - m_tareWeight);

                // 過去のデータを元に補正する
                //m_weightHistory.Add(m_weight);
            }
        }

        /// <summary>
        /// データにフィルタをかけます。
        /// ３個ずつ中央値をとって平滑化します。
        /// </summary>
        /// <param name="arrayIn">入力データ</param>
        /// <param name="nIn">入力データ数</param>
        /// <param name="arrayOut">出力データ</param>
        /// <param name="nOut">出力データ数</param>
        private void DigitalFilterMedian(short[] arrayIn, int nIn, short[] arrayOut, out int nOut)
        {
            if (nIn - 2 > 0)
            {
                // ３個ずつ、中央値を取っていく
                nOut = nIn - 2;
                for (int i = 0; i < nOut; ++i)
                {
                    int a1 = arrayIn[i];
                    int a2 = arrayIn[i + 1];
                    int a3 = arrayIn[i + 2];

                    // a1 ≦ a2 ≦ a3 となるようにソートする
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
        /// データにフィルタをかけます。
        /// ３個ずつ平均値をとって平滑化します。
        /// </summary>
        /// <param name="arrayIn">入力データ</param>
        /// <param name="nIn">入力データ数</param>
        /// <param name="arrayOut">出力データ</param>
        /// <param name="nOut">出力データ数</param>
        private void DigitalFilterAverage(short[] arrayIn, int nIn, short[] arrayOut, out int nOut)
        {
            if (nIn - 2 > 0)
            {
                // ３個ずつ、平均値を取っていく
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
        /// 重さを取得します。
        /// </summary>
        /// <returns>重さ[g]</returns>
        public short GetWeight()
        {
            return m_weight;
        }

#if History
        /// <summary>
        /// 重さ履歴を取得します。
        /// </summary>
        /// <returns>重さの履歴</returns>
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
        /// 重さ履歴の最大数を取得します。
        /// </summary>
        /// <returns>重さ履歴の最大数</returns>
        public int GetWeightHistoryMaxSize()
        {
            return HistorySize;
        }
#endif
        /// <summary>
        /// 個数を取得します。
        /// </summary>
        /// <returns>個数</returns>
        public short GetQuantity()
        {
            return (short) System.Math.Round((double)m_weight / WeightPerQuantity);
        }

        /// <summary>
        /// 現在の重さを0gとします。
        /// </summary>
        public void ResetWeight()
        {
            LoadWeight(true);
        }


        /// <summary>
        /// 0～1の入力値をグラム単位に変換します。
        /// </summary>
        /// <param name="proportion">0～1のアナログ入力値</param>
        /// <returns>重さ[g]</returns>
        private short ConvertProportionToWeight(double proportion)
        {
            const double K = 2060;// 2048; //係数(実測値から求める)

            return (short)(proportion * K);
        }

#if History
        #region ShortRingBuffer
        /// <summary>
        /// リングバッファ
        /// </summary>
        class ShortRingBuffer : IEnumerable
        {
            private short[] m_data;

            private int m_startIndex;

            private int m_endIndex;

            private bool m_isFull;

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="size">サイズ</param>
            public ShortRingBuffer(int size)
            {
                m_data = new short[size];
                m_startIndex = 0;
                m_endIndex = -1;
                m_isFull = false;
            }

            /// <summary>
            /// 内容をクリアします。
            /// </summary>
            public void Clear()
            {
                m_startIndex = 0;
                m_endIndex = -1;
                m_isFull = false;
            }

            /// <summary>
            /// データがいっぱいかどうか
            /// </summary>
            public bool IsFull
            {
                get { return m_isFull; }
            }

            /// <summary>
            /// リングバッファにデータを１つ追加します。
            /// </summary>
            /// <param name="data">データ</param>
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
            /// 反復子を取得します。
            /// </summary>
            /// <returns>反復子</returns>
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
            /// データの個数を返します。
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
