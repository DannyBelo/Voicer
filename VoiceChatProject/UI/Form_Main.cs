﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using Voicer.Properties;
using Voicer.Sound;

namespace Voicer.UI
{
    public partial class Voicer_Main : Form
    {
        Client localClient;
        List<User> UserList;

        Form_Preferences preferences;

        public static Keys PTT_BUTTON = Keys.X;

        //int i = 0;
        Audio recorder;

        Form_Connect connectForm;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        public Voicer_Main()
        {
            AllocConsole();

            InitializeComponent();
            localClient = new Client();
            UserList = new List<User>();
            recorder = new Audio();
            ClientListControl.ListItemClicked += ItemList_Clicked;

            preferences = new Form_Preferences(this);

            // Called when the user list needs to be updated
            localClient.UserListUpdated += OnChannelListUpdate;
            // Called when a chat message is recieved.
            localClient.ChatMessageRecieved += OnChatMessage;
            localClient.ServerMessageRecieved += OnServerMessage;
            // Called when the Client's status has changed.
            localClient.StatusChanged += UpdateStatusLabel;

            if (Settings.Default.Enable_PTT)
                EnablePTT();
        }

        // Called when a key is pressed or released regardless of form focus. etc.
        public void OnKeyHook(object sender, KeyHookEventArgs e)
        {
            if (e.Key == Settings.Default.Audio_PTTKey)
            {
                if (e.KeyDown)
                {
                    StartRecording();
                }
                else
                {
                    recorder.StopRecording();
                }
            }
        }

        public void EnablePTT()
        {
            InterceptKeys.HookKeyboard(new EventHandler<KeyHookEventArgs>(OnKeyHook));
            recorder.StopRecording();
        }

        public void DisablePTT()
        {
            recorder.StopRecording();
            InterceptKeys.UnHookKeyboard();
            StartRecording();
        }


        public void StartRecording()
        {
            try
            {
                recorder.StartRecording(new EventHandler<NAudio.Wave.WaveInEventArgs>(OnRecorded));
            }
            catch (NAudio.MmException)
            {

                MessageBox.Show("You do not have a recording device connected.", "No Recording Device", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Connect to server
        private void connectToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            connectForm = new Form_Connect();

            DialogResult result = connectForm.ShowDialog();
            if (result == DialogResult.OK)
            {
                // Create the client and init the connection
                StatusLabel.Text = "Connecting...";
                switch (localClient.Connect(connectForm.serverIp, connectForm.nickname))
                {
                    case 0:

                        return;

                    case 1:
                        MessageBox.Show("There has been an error, enter a valid input");
                        localClient = null;
                        break;

                    case 2:
                        MessageBox.Show("There has been an error, make sure you have a stable connection");
                        localClient = null;
                        break;
                }
            }
            
        }

        public void OnChatMessage(string text)
        {
            if (chatbox_Output.InvokeRequired)
            {
                chatbox_Output.Invoke(new Action<string>(OnChatMessage), new object[] { text });
                return;
            }

            chatbox_Output.AppendText(text + "\n");
        }

        public void OnServerMessage(string text)
        {
            if (chatbox_Server.InvokeRequired)
            {
                chatbox_Server.Invoke(new Action<string>(OnServerMessage), new object[] { text });
                return;
            }

            chatbox_Server.AppendText(text + "\n");
        }

        private void OnChannelListUpdate(List<Channel> channelList, bool clear)
        {
            if (clear)
            {
                ClientListControl.ClearUsers();
            }
            else
                ClientListControl.SetUsers(channelList);
        }

        public void UpdateStatusLabel(string status)
        {
            if (StatusLabel.InvokeRequired)
            {
                StatusLabel.Invoke(new Action<string>(UpdateStatusLabel), new object[] { status });
                return;
            }

            label1.Text = status;
        }

        // Called when an item in the user list box is clicked
        public void ItemList_Clicked(object sender, EventArgs e)
        {
            ListItem item = (ListItem)sender;
            if(item.channel != null)
                localClient.JoinChannel(item.channel.ID);
        }

        private void Voicer_Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (localClient.IsConnected)
                localClient.Disconnect();
        }

        private void SendChatButton_Click(object sender, EventArgs e)
        {
            if (chatbox_Input.Text != "" || chatbox_Input.Text != null)
            {
                if (ChatTab.SelectedTab == ChatTab_Channel)
                    localClient.SendChatMessage(chatbox_Input.Text);
                else if (ChatTab.SelectedTab == ChatTab_Server)
                    localClient.SendServerMessage(chatbox_Input.Text);

                chatbox_Input.Text = "";
            }
        }

        private void chatbox_Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                if (chatbox_Input.TextLength > 0)
                    SendChatButton_Click(null, EventArgs.Empty);
            }
            else if (chatbox_Input.TextLength > 1024)
                e.SuppressKeyPress = true;
        }

        public void OnRecorded(object sender, NAudio.Wave.WaveInEventArgs e)
        {
            localClient.SendVoiceMessage(e.Buffer);
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!preferences.Visible)
            {
                preferences = new Form_Preferences(this);
                preferences.Show();
            }
            else preferences.BringToFront();
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            localClient.Disconnect();
        }

        private void chatbox_Output_TextChanged(object sender, EventArgs e)
        {
            chatbox_Output.SelectionStart = chatbox_Output.TextLength;
            chatbox_Output.ScrollToCaret();
        }

    }


}
