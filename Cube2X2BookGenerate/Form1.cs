﻿namespace Grayscale.Cube2X2BookGenerate
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;

    /// <summary>
    /// 2x2のキューブ。
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>
        /// 棋譜。
        /// </summary>
        private int[] record;

        /// <summary>
        /// 初期局面から何手目か。
        /// </summary>
        private int ply;

        /// <summary>
        /// 1つ前の盤面。
        /// </summary>
        private string previousBoardText;

        /// <summary>
        /// 定跡。
        /// </summary>
        private Dictionary<string, BookRecord> book;

        /// <summary>
        /// Initializes a new instance of the <see cref="Form1"/> class.
        /// </summary>
        public Form1()
        {
            this.InitializeComponent();
            this.record = new int[100];
            this.book = new Dictionary<string, BookRecord>();
            this.SetNewGame();

            // 定跡読込。
            this.ReadBook();

            this.timer1.Start();
        }

        /// <summary>
        /// ゲーム開始状態に戻します。
        /// </summary>
        public void SetNewGame()
        {
            this.ply = 0;
            this.developmentUserControl1.SetNewGame();
            this.previousBoardText = this.developmentUserControl1.GetBoardText();
        }

        /// <summary>
        /// 定跡読込。
        /// </summary>
        public void ReadBook()
        {
            this.book.Clear();
            if (File.Exists("./book.txt"))
            {
                foreach (var line in File.ReadAllLines("./book.txt"))
                {
                    var tokens = line.Split(' ');

                    // 次の一手。
                    var move = int.Parse(tokens[2], CultureInfo.CurrentCulture);

                    // 手数。
                    var ply = int.Parse(tokens[3], CultureInfo.CurrentCulture);

                    // 既に追加されているやつがあれば、手数を比較する。
                    if (this.book.ContainsKey(tokens[0]))
                    {
                        if (ply < this.book[tokens[0]].Ply)
                        {
                            // 短くなっていれば更新する。
                            this.book[tokens[0]] = new BookRecord(tokens[1], move, ply);
                        }
                    }
                    else
                    {
                        this.book.Add(tokens[0], new BookRecord(tokens[1], move, ply));
                    }
                }
            }
        }

        /// <summary>
        /// 定跡全文。
        /// </summary>
        /// <returns>定跡。</returns>
        public string ToBookText()
        {
            var builder = new StringBuilder();
            foreach (var record in this.book)
            {
                builder.AppendLine(string.Format(
                    CultureInfo.CurrentCulture,
                    "{0} {1}",
                    record.Key,
                    record.Value.ToText()));
            }

            return builder.ToString();
        }

        /// <summary>
        /// より短い手数が発見された。
        /// </summary>
        /// <param name="boardText">その盤面。</param>
        /// <param name="shortPly">最短手数。</param>
        private void UpdateBookRecord(string boardText, int shortPly)
        {
            foreach (var record in this.book)
            {
                if (record.Value.PreviousBoardText == boardText)
                {
                    // 念のため調べておくが、必ず短くなるはず。
                    if (shortPly < record.Value.Ply - 1)
                    {
                        record.Value.Ply = shortPly + 1;

                        // 処理が重くなるが、再帰的に全部調べる。
                        this.UpdateBookRecord(record.Key, record.Value.Ply);
                    }
                }
            }
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            var rand = new Random();

            // 0～11。
            int handle;

            while (true)
            {
                handle = rand.Next(12);

                // 3手続けて同じ個所を回すのは、逆回りに1回回すのと同じなので、そのような手は除外する。
                if (this.ply > 2 && handle == this.record[this.ply - 1] && handle == this.record[this.ply - 2])
                {
                    Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Info: 3連続同じ手は除外。"));
                    continue;
                }

                break;
            }

            // キューブをひねる。
            this.developmentUserControl1.RotateOnly(handle);

            // 正規化した局面を作成。
            // var normalizedPosition = NormalizedPosition.Build(this.developmentUserControl1.DevelopmentTiles);

            // 展開図を正規化する。
            // normalizedPosition.Normalize(this.developmentUserControl1.DevelopmentTiles);

            // 定跡作成。
            // 現盤面 前盤面 指し手 初期局面からの手数
            var currentBoardText = this.developmentUserControl1.GetBoardText();
            var bookRecord = new BookRecord(this.previousBoardText, handle, this.ply);

            bool newRecord = false;
            if (this.book.ContainsKey(currentBoardText))
            {
                var exists = this.book[currentBoardText];
                if (bookRecord.Ply < exists.Ply)
                {
                    // 短い手数が発見された。

                    // 上書き。
                    this.book[currentBoardText] = bookRecord;
                    newRecord = true;

                    // 他の手も 芋づる式に更新できるかもしれない。
                    this.UpdateBookRecord(currentBoardText, bookRecord.Ply);
                }
            }
            else
            {
                this.book.Add(currentBoardText, bookRecord);
                newRecord = true;
            }

            if (newRecord)
            {
                Trace.WriteLine(string.Format(
                    CultureInfo.CurrentCulture,
                    "Write: ./book.txt {0} {1} size({2})",
                    currentBoardText,
                    bookRecord.ToText(),
                    this.book.Count));

                // TODO ばんばん保存。
                File.WriteAllText("./book.txt", this.ToBookText());
            }

            this.record[this.ply] = handle;
            this.ply++;
            this.previousBoardText = currentBoardText;

            // どんな局面からでも 14手 で戻せるらしい。
            // しかし それでは探索が広がらないので、99手まで続けることにする。
            if (this.ply > 99)
            {
                this.SetNewGame();
            }
        }
    }
}