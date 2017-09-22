﻿using KS2Drive.FS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KS2Drive.Debug
{
    public partial class DebugView : Form
    {
        public DebugView(DavFS MFS)
        {
            Host = MFS;

            InitializeComponent();
            Host.DebugMessagePosted += (sender, e) => { DisplayEvent(); };
            listView1.DoubleBuffering(true);
        }

        private DavFS Host;

        private bool PauseDequeue = false;
        private object PauseDequeueLock = new object();

        private bool IsDequeueing = false;
        private object IsDequeueingLock = new object();

        private void DisplayEvent()
        {
            lock (PauseDequeueLock)
            {
                if (PauseDequeue) return;
            }

            lock (IsDequeueingLock)
            {
                if (IsDequeueing) return;
                IsDequeueing = true;
            }

            if (listView1.InvokeRequired)
            {
                listView1.Invoke(new MethodInvoker(delegate { DisplayEventSafe(); }));
            }
            else
            {
                DisplayEventSafe();
            }
        }

        private void DisplayEventSafe()
        {
            while (Host.DebugMessageQueue.TryDequeue(out DebugMessage DM))
            {
                lock (PauseDequeueLock)
                {
                    if (PauseDequeue) break;
                }

                if (DM.MessageType == 1)
                {
                    bool Found = false;

                    for (int i = listView1.Items.Count - 1; i >= Math.Max(0, listView1.Items.Count - 25); i--)
                    {
                        if (listView1.Items[i].SubItems[0].Text.Equals(DM.OperationId))
                        {
                            listView1.Items[i].SubItems[5].Text = DM.date.ToString("HH:MM:ss:ffff");
                            listView1.Items[i].SubItems[6].Text = DM.Result;
                            listView1.Items[i].SubItems[7].Text = Convert.ToInt32((DM.date - (DateTime)listView1.Items[i].SubItems[4].Tag).TotalMilliseconds).ToString();

                            if (!DM.Result.StartsWith("STATUS_SUCCESS")) listView1.Items[i].ForeColor = Color.Red;
                            Found = true;
                            break;
                        }
                    }
                }
                else
                {
                    listView1.Items.Add(new ListViewItem(new String[] { DM.OperationId, DM.Handle, DM.Caller, DM.Path, DM.date.ToString("HH:mm:ss:ffff"), "", "", "" }));
                    listView1.Items[listView1.Items.Count - 1].SubItems[4].Tag = DM.date;
                    listView1.Items[listView1.Items.Count - 1].EnsureVisible();
                }
            }

            lock (IsDequeueingLock)
            {
                IsDequeueing = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Host.DebugMessagePosted -= (ps, pe) => { DisplayEvent(); };
        }

        private void button2_Click(object sender, EventArgs e)
        {
            lock (PauseDequeueLock)
            {
                if (PauseDequeue)
                {
                    button2.Text = "Pause";
                    PauseDequeue = false;
                }
                else
                {
                    button2.Text = "Continue";
                    PauseDequeue = true;
                }
            }

            DisplayEvent();
        }
    }

    public static class ControlExtensions
    {
        public static void DoubleBuffering(this Control control, bool enable)
        {
            var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferPropertyInfo.SetValue(control, enable, null);
        }
    }
}
