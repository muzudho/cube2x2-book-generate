﻿namespace Grayscale.Cube2x2
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows.Forms;

    /// <summary>
    /// 2x2のキューブ。
    /// </summary>
    public partial class Cube2x2 : Form
    {
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
        /// Initializes a new instance of the <see cref="Cube2x2"/> class.
        /// </summary>
        public Cube2x2()
        {
            this.InitializeComponent();
            this.book = new Dictionary<string, BookRecord>();
            this.SetNewGame();
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

        private void Timer1_Tick(object sender, EventArgs e)
        {
            var rand = new Random();

            // 0～11。
            var handle = rand.Next(12);
            this.developmentUserControl1.RotateOnly(handle);

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
                    // 上書き。
                    this.book[currentBoardText] = bookRecord;
                    newRecord = true;
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
                    "{0} {1}",
                    currentBoardText,
                    bookRecord.ToText()));
            }

            this.ply++;
            this.previousBoardText = currentBoardText;

            // どんな局面からでも 14手 で戻せるらしい。
            if (this.ply > 14)
            {
                this.SetNewGame();
            }
        }
    }
}
