﻿using Discord_UWP.SharedModels;
using Microsoft.Advertising.WinRT.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static Discord_UWP.Common;
// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Discord_UWP
{
    public sealed partial class MessageControl : UserControl
    {
        private bool _isadvert = false;
        public bool IsAdvert
        {
            get { return _isadvert; }
            set { _isadvert = value; Notify("IsAdvert"); }
        }
        public static readonly DependencyProperty IsAdvertProperty =
        DependencyProperty.Register("IsAdvert", typeof(bool), typeof(MessageControl),
        new PropertyMetadata(string.Empty, OnIsAdvertPropertyChanged));
        private static void OnIsAdvertPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MessageControl)d)._isadvert = (bool)e.NewValue;
        }


        Message? _currentmessage;
        public Message? Message
        {
            get { return _currentmessage; }
            set { _currentmessage = value; Notify("message"); }
        }
        public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register("Message", typeof(Message?), typeof(MessageControl),
        new PropertyMetadata(null, OnmessagePropertyChanged));
        private static void OnmessagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((Message?) e.NewValue == null) return;
            Debug.WriteLine("New message");
            ((MessageControl)d)._currentmessage = (Message?)e.NewValue;
            ((MessageControl)d).UpdateControl();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void UpdateControl()
        {
            if (_currentmessage?.Id == null)
            {
                Debug.WriteLine("message is null");
                lastColumn.Width = new GridLength(0);
                AdControl ad = new AdControl();
                ad.HorizontalAlignment = HorizontalAlignment.Center;
                ad.Width = 300;
                ad.Height = 50;
                ad.ApplicationId = "d9818ea9-2456-4e67-ae3d-01083db564ee";
                ad.AdUnitId = "336795";
                this.Tag = "Ad";
                ad.Margin = new Thickness(6);
                rootGrid.Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
                Grid.SetColumn(ad, 1);
                Grid.SetRow(ad, 1);
                rootGrid.Children.Add(ad);
                return;
            }

            username.Text = _currentmessage.Value.User.Username;
                SharedModels.GuildMember member;
                if (Storage.Cache.Guilds[App.CurrentId].Members.ContainsKey(Message.Value.User.Id))
                {
                    member = Storage.Cache.Guilds[App.CurrentId].Members[Message.Value.User.Id].Raw;
                }
                else
                {
                    member = new SharedModels.GuildMember();
                }
                if (member.Nick != null)
                {
                    username.Text = member.Nick;
                }

            if (member.Roles != null && member.Roles.Count() > 0)
            {
                foreach (SharedModels.Role role in Storage.Cache.Guilds[App.CurrentId].RawGuild.Roles)
                {
                    if (role.Id == member.Roles.First<string>())
                    {
                        username.Foreground = IntToColor(role.Color);
                    }
                }
            }

            if (_currentmessage.Value.User.Bot == true)
                BotIndicator.Visibility = Visibility.Visible;

            if (!string.IsNullOrEmpty(_currentmessage.Value.User.Avatar))
            {
                avatar.Fill = new ImageBrush() { ImageSource = new BitmapImage(new Uri("https://cdn.discordapp.com/avatars/" + _currentmessage.Value.User.Id + "/" + _currentmessage.Value.User.Avatar + ".jpg")) };
            }
            else
            {
                avatar.Fill = new ImageBrush() { ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Square150x150Logo.scale-200.png")) };
            }

            timestamp.Text = Common.HumanizeDate(_currentmessage.Value.Timestamp, null);
            if (_currentmessage.Value.EditedTimestamp.HasValue)
                timestamp.Text += " (Edited " + Common.HumanizeDate(_currentmessage.Value.EditedTimestamp.Value, _currentmessage.Value.Timestamp) + ")";

            if (Message.Value.Reactions != null)
            {
                GridView gridview = new GridView
                {
                    SelectionMode = ListViewSelectionMode.None,
                    Margin = new Thickness(0),
                    Padding = new Thickness(0)
                };
                foreach (Reactions reaction in Message.Value.Reactions.Where(x => x.Count>0))
                {
                    
                    ToggleButton gridviewitem = new ToggleButton();
                    gridviewitem.IsChecked = reaction.Me;
                    gridviewitem.Tag = new Tuple<string, string, SharedModels.Reactions>(Message.Value.ChannelId, Message.Value.Id, reaction);
                    gridviewitem.Click += ToggleReaction;
                    if (reaction.Me)
                    {
                        gridviewitem.IsChecked = true;
                    }

                    StackPanel stack = new StackPanel();
                    stack.Orientation = Orientation.Horizontal;

                    TextBlock textblock = new TextBlock();
                    textblock.Text = reaction.Emoji.Name + " " + reaction.Count.ToString();
                    stack.Children.Add(textblock);
                    gridviewitem.Content = stack;
                    gridviewitem.Style = (Style)App.Current.Resources["EmojiButton"];
                    gridview.Items.Add(gridviewitem);
                }
                Grid.SetRow(gridview, 2);
                Grid.SetColumn(gridview, 1);
                rootGrid.Children.Add(gridview);
            }

            string text = _currentmessage.Value.Content;
            foreach(var m in _currentmessage.Value.Mentions)
            {
                text = text.Replace(m.Id, "");
            }
            content.Text = _currentmessage.Value.Content;
        }
        public MessageControl()
        {
            this.InitializeComponent();

        }

        private void moreButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as Button);
        }

        private void UserControl_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(moreButton);
        }

        private void UserControl_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(moreButton);
        }

        private void TypingStarted(RichEditBox sender, RichEditBoxTextChangingEventArgs args)
        {

        }

        private void CreateMessage(object sender, RoutedEventArgs e)
        {

        }

        private void MessageBox_LostFocus(object sender, RoutedEventArgs e)
        {
            EditBox.Visibility = Visibility.Collapsed;
        }
        private void ToggleReaction(object sender, RoutedEventArgs e)
        {
            if ((sender as ToggleButton)?.IsChecked == false) //Inverted since it changed
            {
                Session.DeleteReaction(((sender as ToggleButton).Tag as Tuple<string, string, SharedModels.Reactions>)?.Item1, ((sender as ToggleButton).Tag as Tuple<string, string, SharedModels.Reactions>)?.Item2, ((Tuple<string, string, Reactions>) (sender as ToggleButton).Tag).Item3.Emoji);

                if (((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Me)
                {
                    ((ToggleButton) sender).Content = (((ToggleButton) sender).Tag as Tuple<string, string, SharedModels.Reactions>)?.Item3.Emoji.Name + " " + (((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Count - 1).ToString();
                }
                else
                {
                    ((ToggleButton) sender).Content = (((ToggleButton) sender).Tag as Tuple<string, string, SharedModels.Reactions>)?.Item3.Emoji.Name + " " + (((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Count).ToString();
                }
            }
            else
            {
                Session.CreateReaction((((ToggleButton) sender).Tag as Tuple<string, string, SharedModels.Reactions>)?.Item1, ((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item2, ((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Emoji);

                if (((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Me)
                {
                    ((ToggleButton) sender).Content = (((ToggleButton) sender).Tag as Tuple<string, string, SharedModels.Reactions>)?.Item3.Emoji.Name + " " + (((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Count).ToString();
                }
                else
                {
                    ((ToggleButton) sender).Content = ((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Emoji.Name + " " + (((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Count + 1).ToString();
                }
            }
        }
        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            string val = "";
            MessageBox.Document.GetText(TextGetOptions.None, out val);
            MessageBox.Document.SetText(TextSetOptions.None, val);
            EditBox.Visibility = Visibility.Visible;
            MessageBox.Focus(FocusState.Programmatic);
        }
    }
}
