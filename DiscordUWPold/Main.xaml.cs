﻿/* Style Guide!!! (We need this)
 * Comments:
 * //Is used to comment-out any code while \**\ is used for notes
 * Old functions in a file (commented or active) should be put in a Region called OldCode and placed at the bottom of the file
 * If the commented code is midfunction just put new in Region OldCode around it
 * 
 * Object naming:
 * Properties and class instance object should be capitalized
 * Function specific objects should be lowercased with caps inplace of space 
 * Property objects should be named with an underscore infront then lowercase
 * 
 * Properties:
 * if a Property {get;} only returns the object call the Property (this is for Get References)
 * 
 * Random:
 * App.CurrentGuild and then App.CurrentGuildId should be used instead of ServerList.SelectedItem in everycase where it is not necessary
 * SharedModels and CacheModels should be included in all files that use them
 * CacheModels overrule SharedModels
 * Use lambdas meaningfully, nothing is dumber than excessive lambda use
*/
using Discord_UWP.API;
using Discord_UWP.API.User;
using Discord_UWP.API.User.Models;
using Discord_UWP.Authentication;
using Microsoft.Advertising.WinRT.UI;
using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Store;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.UI;
using Windows.Services.Store;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using static Discord_UWP.Common;
using Discord_UWP.CacheModels;
using Discord_UWP.Gateway.DownstreamEvents;
using Discord_UWP.Gateway;
using Discord_UWP.SharedModels;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI.Animations;

#region CacheModels Overrule
using GuildChannel = Discord_UWP.CacheModels.GuildChannel;
using Message = Discord_UWP.CacheModels.Message;
using User = Discord_UWP.CacheModels.User;
using Guild = Discord_UWP.CacheModels.Guild;
using Windows.UI.Xaml.Media.Animation;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.DataTransfer;
#endregion



/* The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238 */

namespace Discord_UWP
{
    /*<summary>
     An empty page that can be used on its own or navigated to within a Frame.
     </summary>*/
    public sealed partial class Main : Page
    {
        public async void Login(string args = null)
        {
            LoadingSplash.Show(false);
            try
            {
                var licenseInformation = CurrentApp.LicenseInformation;
                if (licenseInformation.ProductLicenses["RemoveAds"].IsActive)
                {
                    App.ShowAds = false;
                }

                LoadingSplash.Status = App.GetString("/Main/LoggingIn");
                await Session.AutoLogin();
                Session.Online = true;
                EstablishGateway();
                LoadingSplash.Show(false);
                LoadingSplash.Status = App.GetString("/Main/Loading");

                LoadMessages();
                LoadMutedChannels();

                await LoadUser();
                LoadGuilds();
                LoadingSplash.Status = App.GetString("/Main/Connected");
                await Task.Delay(1000);
            }
            catch
            {
                LoadingSplash.Hide(false);
                App.ShowAds = false;
                IAPSButton.Visibility = Visibility.Collapsed;
                await LoadCache();
                LoadGuilds();
                LoadMessages();
                LoadMutedChannels();

                await LoadUser();

                LoadingSplash.Status = App.GetString("Main/Offline").ToUpper();
                await Task.Delay(3000);
                Session.Online = false;
                LoadingSplash.Hide(false);
            }
            if(args != null)
            {
                bool guildId = true;
                foreach (char c in args)
                {
                    if (c == ':')
                    {
                        guildId = false;
                    }
                    else if (guildId)
                    {
                        SelectGuildId += c;
                    }
                    else
                    {
                        SelectChannelId += c;
                    }
                }
                SelectChannel = true;
            }
            if (Servers.DisplayMode == SplitViewDisplayMode.CompactOverlay || Servers.DisplayMode == SplitViewDisplayMode.Overlay)
            {
                Servers.IsPaneOpen = true;
                ContentCache.Opacity = 0.5;
            }
            //App.PlayHeartBeat(); //Test
        }

        public Main()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && e.Parameter.ToString() != "")
            {
                Login(e.Parameter.ToString());
            } else
            {
                Login();
            }

            SetupUI();
        }

        private bool VibrationEnabled = true;
        private void SetupUI()
        {
            var view = CoreApplication.GetCurrentView();
            //view.TitleBar.LayoutMetricsChanged += TitleBar_LayoutMetricsChanged;
            view.TitleBar.ExtendViewIntoTitleBar = false;
            //TitleBarContent.Height = view.TitleBar.Height;
            //CompactOverlayToggle.Margin = new Thickness(0, 0, view.TitleBar.SystemOverlayRightInset, 0);
            //Window.Current.SetTitleBar(DraggableTitleBar);
            var info = new DrillInNavigationTransitionInfo();
            TransitionCollection collection = new TransitionCollection();
            NavigationThemeTransition theme = new NavigationThemeTransition();
            NavigationCacheMode = NavigationCacheMode.Disabled;
            theme.DefaultNavigationTransitionInfo = info;
            collection.Add(theme);
            SubFrame.ContentTransitions = collection;

            App.MenuHandler += ShowMenu;
            Storage.SettingsChangedHandler += SettingsChanged;
            App.NavigateToCreateServerHandler += OnNavigateToCreateServer;
            App.NavigateToJoinServerHandler += OnNavigateToJoinServer;
            App.SubpageClosedHandler += SubpageClosed;
            App.LinkClicked += MessageControl_OnLinkClicked;
            App.OpenAttachementHandler += OpenAttachement;
            App.NavigateToProfileHandler += OnNavigateToProfile;
            App.ShowMemberFlyoutHandler += OnShowMemberFlyoutHandler;
            App.NavigateToGuildChannelHandler += OnNavigateToGuildChannel;
            App.NavigateToDMChannelHandler += OnNavigateToDMChannel;
            App.NavigateToChannelEditHandler += OnNavigateToChannelEdit;
            App.NavigateToDeleteChannelHandler += OnNavigateToDeleteChannel;
            App.NavigateToGuildEditHandler += OnNavigateToGuildEdit;
            App.NavigateToNicknameEditHandler += OnNavigateToNicknameEdit;
            App.NavigateToCreateBanHandler += OnNavigateToCreateBan;
            App.NavigateToLeaveServerHandler += OnNavigateToLeaverServer;
            App.NavigateToDeleteServerHandler += OnNavigateToDeleteServer;
            App.MentionHandler += OnMention;
            App.UpdateUnreadIndicatorsHandler += OnUpdateUnreadIndicators;
            App.ConnectoToVoiceHandler += App_ConnectoToVoiceHandler;
            App.PlayHeartBeatHandler += App_PlayHeartBeatHandler;
            App.AckLastMessage += App_AckLastMessage;
            App.NavigateToBugReportHandler += App_NavigateToBugReportHandler;
            Session.VoiceConnection.VoiceDataRecieved += VoiceConnection_VoiceDataRecieved;
            SettingsChanged(null, null);
        }

        private void App_NavigateToBugReportHandler(object sender, App.BugReportNavigationArgs e)
        {
            SubFrameNavigator(typeof(SubPages.BugReport), e.Exception);
        }

        private void VoiceConnection_VoiceDataRecieved(object sender, Voice.VoiceConnectionEventArgs<Voice.DownstreamEvents.VoiceData> e)
        {

        }

        private async void App_AckLastMessage(object sender, EventArgs e)
        {
            try
            {
                if (Messages.Items.Any())
                {
                    var lastmessage = (MessageContainer)Messages.Items.Last();
                    await Task.Run(async () =>
                    {
                        await Session.AckMessage(lastmessage.Message.Value.ChannelId, lastmessage.Message.Value.Id);
                    });
                }
            }
            catch (Exception exception)
            {
                SubFrameNavigator(typeof(SubPages.BugReport), exception);
            }
        }

        private async void App_PlayHeartBeatHandler(object sender, EventArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                   () =>
                   {
                       Heartbeat.Play();
                   });
        }

        private async void App_ConnectoToVoiceHandler(object sender, App.ConnectToVoiceArgs e)
        {
            await Session.Gateway.VoiceStatusUpdate(e.GuildId, e.ChannelId, true, false);
        }

        private void OnUpdateUnreadIndicators(object sender, EventArgs e)
        {
            UpdateGuildAndChannelUnread();
        }

        private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            TitleBarContent.Height = sender.Height;
            CompactOverlayToggle.Margin = new Thickness(0, 0, sender.SystemOverlayRightInset, 0);
        }

        private void OnNavigateToJoinServer(object sender, EventArgs e)
        {
            SubFrameNavigator(typeof(SubPages.JoinServer));
        }

        private void OnNavigateToCreateServer(object sender, EventArgs e)
        {
            SubFrameNavigator(typeof(SubPages.CreateServer));
        }

        private void OnNavigateToDeleteChannel(object sender, App.DeleteChannelNavigationArgs e)
        {
            SubFrameNavigator(typeof(SubPages.DeleteChannel), e.ChannelId);
        }

        private void OnNavigateToDeleteServer(object sender, App.DeleteServerNavigationArgs e)
        {
            SubFrameNavigator(typeof(SubPages.DeleteServer), e.GuildId);
        }

        private void OnNavigateToLeaverServer(object sender, App.LeaverServerNavigationArgs e)
        {
            SubFrameNavigator(typeof(SubPages.LeaveServer), e.GuildId);

        }

        private void OnNavigateToCreateBan(object sender, App.CreateBanNavigationArgs e)
        {
            SubFrameNavigator(typeof(SubPages.CreateBan), e.UserId);
        }

        private void OnNavigateToNicknameEdit(object sender, App.NicknameEditNavigationArgs e)
        {
            SubFrameNavigator(typeof(SubPages.EditNickname), e.UserId);
        }

        private void OnNavigateToGuildEdit(object sender, App.GuildEditNavigationArgs e)
        {
            SubFrameNavigator(typeof(SubPages.EditGuild), e.GuildId);
        }

        private async void OnNavigateToDMChannel(object sender, App.DMChannelNavigationArgs e)
        {
            SelectChannel = true;
            SelectChannelId = e.UserId;
            ServerList.SelectedIndex = 0;
            if (e.Message != null && e.Send)
            {
                await Session.CreateMessage(App.CurrentChannelId, e.Message);
            } else if (e.Message != null && !e.Send)
            {
                MessageBox1.Text = e.Message;
            }
        }

        private void SettingsChanged(object sender, EventArgs e)
        {
            if (Storage.Settings.AppBarAtBottom)
                VisualStateManager.GoToState(this, "AppBarAlignment_Bottom", false);
            else
                VisualStateManager.GoToState(this, "AppBarAlignment_Top", false);

            var settings = Storage.Settings;;
            ResponsiveUI_M_Trigger.MinWindowWidth = settings.RespUiM;
            ResponsiveUI_L_Trigger.MinWindowWidth = settings.RespUiL;
            ResponsiveUI_XL_Trigger.MinWindowWidth = settings.RespUiXl;
            VibrationEnabled = settings.Vibrate;
        }

        async void EstablishGateway()
        {
            Session.Gateway.Ready += OnReady;
            Session.Gateway.MessageCreated += MessageCreated;
            Session.Gateway.MessageDeleted += MessageDeleted;
            Session.Gateway.MessageUpdated += MessageUpdated;
            Session.Gateway.MessageReactionAdded += MessageReactionAdded;
            Session.Gateway.MessageReactionRemoved += MessageReactionRemoved;
            Session.Gateway.MessageReactionRemovedAll += MessageReactionRemovedAll;
            Session.Gateway.MessageAck += OnMessageAck;

            Session.Gateway.GuildCreated += GuildCreated;
            Session.Gateway.GuildDeleted += GuildDeleted;
            Session.Gateway.GuildUpdated += GuildUpdated;

            Session.Gateway.GuildChannelCreated += GuildChannelCreated;
            Session.Gateway.GuildChannelDeleted += GuildChannelDeleted;
            Session.Gateway.GuildChannelUpdated += GuildChannelUpdated;

            Session.Gateway.GuildMemberAdded += GuildMemberAdded;
            Session.Gateway.GuildMemberRemoved += GuildMemberRemoved;
            Session.Gateway.GuildMemberUpdated += GuildMemberUpdated;
            Session.Gateway.GuildMemberChunk += GuildMemberChunked;

            Session.Gateway.DirectMessageChannelCreated += DirectMessageChannelCreated;
            Session.Gateway.DirectMessageChannelDeleted += DirectMessageChannelDeleted;

            Session.Gateway.PresenceUpdated += PresenceUpdated;
            Session.Gateway.TypingStarted += TypingStarted;

            Session.Gateway.RelationShipAdded += RelationShipAdded;
            Session.Gateway.RelationShipRemoved += RelationShipRemoved;
            Session.Gateway.RelationShipUpdated += RelationShipUpdated;

            Session.Gateway.UserNoteUpdated += UserNoteUpdated;
            Session.Gateway.UserSettingsUpdated += GatewayOnUserSettingsUpdated;

            Session.Gateway.VoiceStateUpdated += OnVoiceStateUpdated;
            Session.Gateway.VoiceServerUpdated += OnVoiceServerUpdated;

            try
            {
                await Session.Gateway.ConnectAsync();
                Session.SlowSpeeds = false;
                RefreshButton.Visibility = Visibility.Collapsed;
            }
            catch
            {
                Session.SlowSpeeds = true;
                RefreshButton.Visibility = Visibility.Visible;
            }
        }

        private void OnShowMemberFlyoutHandler(object sender, App.ProfileNavigationArgs profileNavigationArgs)
        {
            ShowUserDetails(sender, profileNavigationArgs.User);
        }

        private void OnNavigateToProfile(object sender, App.ProfileNavigationArgs e)
        {
            SubFrameNavigator(typeof(SubPages.UserProfile), e.User.Id);
        }

        private void OnNavigateToChannelEdit(object sender, App.ChannelEditNavigationArgs e)
        {
            SubFrameNavigator(typeof(SubPages.EditChannel), e.ChannelId);
        }

        private void OpenAttachement(object sender, Attachment e)
        {
            SubFrameNavigator(typeof(SubPages.PreviewAttachement), e);
        }

        private void OnMention(object sender, App.MentionArgs e)
        {
            if (MessageBox1.Text.LastOrDefault() == ' ')
            {
                MessageBox1.Text += " ";
            }
            MessageBox1.Text += "@" + e.Username + "#" + e.Discriminator;
        }

        private void OnNavigateToGuildChannel(object sender, App.GuildChannelNavigationArgs e)
        {
            foreach (SimpleGuild guild in ServerList.Items)
            {
                if (guild.Id == e.GuildId)
                {
                    ServerList.SelectedItem = guild;
                }
            }
            SelectChannel = true;
            SelectChannelId = e.ChannelId;
        }

        #region LoadUser
        private async Task LoadUser()
        {
            var currentUser = Storage.Cache.CurrentUser;
            if (currentUser != null)
            {
                Username.Text = currentUser.Raw.Username;
                App.CurrentUserId = currentUser.Raw.Id;
                Discriminator.Text = "#" + currentUser.Raw.Discriminator;
                LargeUsername.Text = Username.Text;
                LargeDiscriminator.Text = Discriminator.Text;
            }

            if (Session.Online)
            {
                await DownloadUser();
            } else
            {
                /*disable online functions*/
            }
        }
        private async Task DownloadUser()
        {
            Storage.Cache.CurrentUser = new User(await Session.GetCurrentUser());
            var currentUser = Storage.Cache.CurrentUser;
            Username.Text = currentUser.Raw.Username;
            App.CurrentUserId = currentUser.Raw.Id;
            Discriminator.Text = "#" + currentUser.Raw.Discriminator;
            LargeUsername.Text = Username.Text;
            LargeDiscriminator.Text = Discriminator.Text;

            ImageBrush image = new ImageBrush() {ImageSource = new BitmapImage(new Uri("https://cdn.discordapp.com/avatars/" + currentUser.Raw.Id + "/" + currentUser.Raw.Avatar + ".jpg"))};
            Avatar.Fill = image;
            LargeAvatar.Fill = image;
            Storage.SaveCache();
        }
        #endregion

        #region LoadGuilds
        private void LoadGuilds()
        {
            ServerList.Items.Clear();
            LoadGuildList();

            if (SelectChannel)
            {
                foreach (SimpleGuild guild in ServerList.Items)
                {
                    if (guild.Id == SelectGuildId)
                    {
                        ServerList.SelectedItem = guild;
                    }
                }
            } else
            {
                ServerList.SelectedIndex = 0;
            }
        }
        #endregion

        #region LoadGuild

        Dictionary<string, Member> memberscvs = new Dictionary<string, Member>();
        private async void CatchServerSelection(object sender, SelectionChangedEventArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                   () =>
                   {
                       if (ServerList.SelectedItem != null) /*Called upon clearing*/
                       {
                           PinnedMessageToggle.Visibility = Visibility.Collapsed;
                           _onlyAllowOpeningPane = true;
                           ToggleServerListFull(null, null);
                           ServerName.Text = (ServerList.SelectedItem as SimpleGuild).Name;
                           TextChannels.Items.Clear();
                           VoiceChannels.Items.Clear();
                           Messages.Items.Clear();
                           Typers.Clear();
                           MembersCvs.Source = null;
                           App.GuildMembers = null;
                           SendMessage.Visibility = Visibility.Collapsed;
                           if ((ServerList.SelectedItem as SimpleGuild).IsDM)
                           {
                               App.CurrentGuildIsDM = true;
                               friendPanel.Visibility = Visibility.Visible;
                               Channels.Visibility = Visibility.Collapsed;
                               DMs.Visibility = Visibility.Visible;
                               headerButton.Visibility = Visibility.Collapsed;
                               MemberListToggle.Visibility = Visibility.Collapsed;
                               Members.Visibility = Visibility.Collapsed;
                               if (Session.Online)
                               {
                                   ChannelsLoading.IsActive = true;
                                   LoadDMs();
                               }
                               else
                               {
                                   LoadDMs();
                               }
                               if (SelectChannel)
                               {
                                   foreach (SimpleChannel channel in DirectMessageChannels.Items)
                                   {
                                       if (channel.Type == 1 && Storage.Cache.DMs[channel.Id].Raw.Users.FirstOrDefault().Id == SelectChannelId)
                                       {
                                           DirectMessageChannels.SelectedItem = channel;
                                           SelectChannel = false;
                                       }
                                   }
                                   if (SelectChannel)
                                   {
                                       Session.CreateDM(new CreateDM() { Recipients = new List<string>() { SelectChannelId } });
                                   }
                               }
                           }
                           else
                           {
                               App.CurrentGuildIsDM = false;
                               Channels.Visibility = Visibility.Visible;
                               DMs.Visibility = Visibility.Collapsed;
                               friendPanel.Visibility = Visibility.Collapsed;
                               MemberListToggle.Visibility = Visibility.Visible;
                               Members.Visibility = Visibility.Visible;
                               if (Session.Online)
                               {
                                   ChannelsLoading.IsActive = true;
                                   DownloadGuild((ServerList.SelectedItem as SimpleGuild).Id);
                               }
                               else
                               {
                                   LoadGuild((ServerList.SelectedItem as SimpleGuild).Id);
                               }
                               if (SelectChannel)
                               {
                                   foreach (SimpleChannel channel in TextChannels.Items)
                                   {
                                       if (channel.Id == SelectChannelId)
                                       {
                                           TextChannels.SelectedItem = channel;
                                       }
                                   }
                                   SelectChannel = false;
                               }
                           }
                       }
                   });
        }

        #region Members
        public async void LoadMembers(string id)
        {
                if (Session.Online)
                {
                    IEnumerable<GuildMember> members = await Session.GetGuildMembers(id);

                    if (members != null)
                    {
                        foreach (GuildMember member in members)
                        {
                            if (Storage.Cache.Guilds[id].Members.ContainsKey(member.User.Id))
                            {
                                Storage.Cache.Guilds[id].Members[member.User.Id] = new Member(member);
                            }
                            else
                            {
                                Storage.Cache.Guilds[id].Members.Add(member.User.Id, new Member(member));
                            }
                        }
                        App.GuildMembers = Storage.Cache.Guilds[id].Members;
                    }
                }
                int totalrolecounter = 0;

            if (Storage.Cache.Guilds[id].RawGuild.Roles != null)
            {
                foreach (Role role in Storage.Cache.Guilds[id].RawGuild.Roles)
                {
                    Role roleAlt = role;
                    if (role.Hoist)
                    {
                        int rolecounter = 0;
                        foreach (Member m in Storage.Cache.Guilds[id].Members.Values)
                            if (m.Raw.Roles.FirstOrDefault() == role.Id) rolecounter++;
                        totalrolecounter += rolecounter;
                        roleAlt.MemberCount = rolecounter;
                    }
                    if (Storage.Cache.Guilds[id].Roles.ContainsKey(role.Id))
                    {
                        Storage.Cache.Guilds[id].Roles[role.Id] = roleAlt;
                    }
                    else
                    {
                        Storage.Cache.Guilds[id].Roles.Add(role.Id, roleAlt);
                    }
                }
                int everyonecounter = Storage.Cache.Guilds[id].Members.Count() - totalrolecounter;
                memberscvs = Storage.Cache.Guilds[id].Members;
                foreach (Member m in memberscvs.Values)
                {
                    if (m.Raw.Roles.FirstOrDefault() != null &&
                        Storage.Cache.Guilds[id].Roles[m.Raw.Roles.FirstOrDefault()].Hoist)
                    {
                        m.MemberDisplayedRole = GetRole(m.Raw.Roles.FirstOrDefault(), id, everyonecounter);
                    }
                    else
                    {
                        m.MemberDisplayedRole = GetRole(null, id, everyonecounter);
                    }
                    if (Session.PrecenseDict.ContainsKey(m.Raw.User.Id))
                    {
                        m.status = Session.PrecenseDict[m.Raw.User.Id];
                    }
                    else
                    {
                        m.status = new Presence() {Status = "offline", Game = null};
                    }
                }
                try
                {
                    var sortedMembers =
                        memberscvs.GroupBy(m => m.Value.MemberDisplayedRole).OrderByDescending(x => x.Key.Position);

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                           // MembersCVS = new CollectionViewSource();
                            MembersCvs.Source = sortedMembers;
                        });
                }
                catch (Exception exception)
                {
                    SubFrameNavigator(typeof(SubPages.BugReport), exception);
                }

                //else
                //    MembersCVS.Source = memberscvs.SkipWhile(m => m.Value.status.Status == "offline").GroupBy(m => m.Value.MemberDisplayedRole).OrderBy(m => m.Key.Position).ToList();
                TempRoleCache.Clear();
            }
        }

        public async void UpdateMember(string memberId)
        {
            memberscvs[memberId].Raw = Storage.Cache.Guilds[App.CurrentGuildId].Members[memberId].Raw;
            if (Session.PrecenseDict.ContainsKey(memberscvs[memberId].Raw.User.Id))
            {
                memberscvs[memberId].status = Session.PrecenseDict[memberscvs[memberId].Raw.User.Id];
            }
            else
            {
                memberscvs[memberId].status = new Presence() { Status = "offline", Game = null };
            }
            try
            {
                var sortedMembers =
                    memberscvs.GroupBy(m => m.Value.MemberDisplayedRole).OrderByDescending(x => x.Key.Position);

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                            // MembersCVS = new CollectionViewSource();
                            MembersCvs.Source = sortedMembers;
                    });
            }
            catch (Exception exception)
            {
                SubFrameNavigator(typeof(SubPages.BugReport), exception);
            }
        }

        private List<DisplayedRole> TempRoleCache = new List<DisplayedRole>(); //This is as a temporary cache of roles to improve performance and not call Storage for every member
        private DisplayedRole GetRole(string roleid, string guildid, int everyonecounter)
        {
            var cachedRole = TempRoleCache.FirstOrDefault(x => x.Id == roleid);
            if (cachedRole != null) return cachedRole;
            else
            {
                DisplayedRole role;
                if (roleid == null || !Storage.Cache.Guilds[guildid].Roles[roleid].Hoist)
                {

                    role = new DisplayedRole(null, 0, App.GetString("/Main/Everyone"), everyonecounter, (SolidColorBrush)App.Current.Resources["Foreground"]);
                    TempRoleCache.Add(role);
                }
                else
                {
                    var storageRole = Storage.Cache.Guilds[guildid].Roles[roleid];
                    role = new DisplayedRole(roleid, storageRole.Position, storageRole.Name.ToUpper(), storageRole.MemberCount, IntToColor(storageRole.Color));
                    TempRoleCache.Add(role);
                }
                return role;
            }
        }

        #endregion

        #region Guild
        private void LoadGuild(string id)
        {
            if (Storage.Cache.Guilds[id] != null)
            {
                App.CurrentGuildId = id;
                ChannelsLoading.IsActive = true;

                Messages.Items.Clear();

                if (!Storage.Cache.Guilds[id].perms.Perms.ManageChannels && !Storage.Cache.Guilds[id].perms.Perms.Administrator && Storage.Cache.Guilds[id].RawGuild.OwnerId != Storage.Cache.CurrentUser.Raw.Id)
                {
                    AddChannelButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    AddChannelButton.Visibility = Visibility.Visible;
                }

                #region Roles
                LoadMembers(id);
                #endregion

                Task.Run(() =>
                {
                    if (Storage.Cache.Guilds[id].RawGuild.Presences != null)
                    {
                        foreach (Presence presence in Storage.Cache.Guilds[id].RawGuild.Presences)
                        {
                            if (Session.PrecenseDict.ContainsKey(presence.User.Id))
                            {
                                Session.PrecenseDict.Remove(presence.User.Id);
                            }
                            Session.PrecenseDict.Add(presence.User.Id, presence);
                        }
                    }
                });


                List<UIElement> channelListBuffer = new List<UIElement>();
                while (channelListBuffer.Count < 1000)
                {
                    channelListBuffer.Add(new Grid());
                }

                LoadChannelList(new List<int>() { 0, 2 });
            }
            else
            {

            }

            ChannelsLoading.IsActive = false;
            MessageBox1.IsEnabled = false;
            if (TextChannels.Items.Count > 0)
            {
                NoGuildChannelsCached.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoGuildChannelsCached.Visibility = Visibility.Visible;
            }
        }
        private void DownloadGuild(string id)
        {
            Messages.Items.Clear();
            TextChannels.Items.Clear();
            VoiceChannels.Items.Clear();

            if (!Storage.Cache.Guilds[id].perms.Perms.ManageChannels && !Storage.Cache.Guilds[id].perms.Perms.Administrator && Storage.Cache.Guilds[id].RawGuild.OwnerId != Storage.Cache.CurrentUser.Raw.Id)
            {
                AddChannelButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                AddChannelButton.Visibility = Visibility.Visible;
            }

            #region Roles
            //await Session.Gateway.RequestAllGuildMembers(id);
            LoadMembers(id);
            App.CurrentGuildId = id;
            #endregion

            Task.Run(() =>
            {
                if (Storage.Cache.Guilds[id].RawGuild.Presences != null)
                    foreach (Presence presence in Storage.Cache.Guilds[id].RawGuild.Presences)
                    {
                        if (Session.PrecenseDict.ContainsKey(presence.User.Id))
                        {
                            Session.PrecenseDict.Remove(presence.User.Id);
                        }
                        Session.PrecenseDict.Add(presence.User.Id, presence);
                    }
            });

            #region Channels

            LoadChannelList(new List<int>(){ 0, 2 });
            #endregion

            ChannelsLoading.IsActive = false;
        }
        #endregion

        #region DMs
        private void LoadDMs()
        {
            DMsLoading.IsActive = true;
            MembersCvs.Source = null;
            PinnedMessageToggle.Visibility = Visibility.Collapsed;
            SendMessage.Visibility = Visibility.Collapsed;
            //MuteToggle.Visibility = Visibility.Collapsed;
            DirectMessageChannels.Items.Clear();
            TextChannels.Items.Clear();
            VoiceChannels.Items.Clear();
            LoadChannelList(new List<int>() { 1, 3 });
            DMsLoading.IsActive = false;
            if (DirectMessageChannels.Items.Count > 0)
            {
                NoDMSCached.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoDMSCached.Visibility = Visibility.Visible;
            }

            App.CurrentGuildId = null;
        }
        #endregion
        #endregion

        #region LoadChannel
        private async void LoadChannelMessages(object sender, SelectionChangedEventArgs e)
        {
            if (TextChannels.SelectedItem != null) /*Called upon clear*/
            {
                friendPanel.Visibility = Visibility.Collapsed;
                SendMessage.Visibility = Visibility.Visible;
                Messages.Visibility = Visibility.Visible;
                headerButton.Visibility = Visibility.Visible;
                if (!App.CurrentGuildIsDM)
                {
                    try
                    {
                        App.CurrentGuild = Storage.Cache.Guilds[(ServerList.SelectedItem as SimpleGuild).Id];
                    }
                    catch
                    {
                        ServerList.SelectedIndex = 1;
                    }
                }
                App.CurrentGuildIsDM = false;
                App.CurrentChannelId = ((SimpleChannel)TextChannels.SelectedItem).Id;
                if (Session.Online)
                {
                    Session.Gateway.SubscribeToGuild(new string[] { App.CurrentGuildId });
                }
                UpdateTypingUI();
                if (Servers.DisplayMode == SplitViewDisplayMode.CompactOverlay || Servers.DisplayMode == SplitViewDisplayMode.Overlay)
                    Servers.IsPaneOpen = false;
                MessagesLoading.Visibility = Visibility.Visible;
                SendMessage.Visibility = Visibility.Visible;
                //MuteToggle.Tag = App.CurrentChannelId;
                //MuteToggle.IsChecked = Storage.MutedChannels.Contains(App.CurrentChannelId);
                //MuteToggle.Visibility = Visibility.Visible;
                PinnedMessageToggle.Visibility = Visibility.Visible;

                Messages.Items.Clear();

                Messages.Items.Add(new MessageControl()); /*Necessary for no good reason*/

                int adCheck = 5;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        foreach (KeyValuePair<string, Message> message in App.CurrentGuild.Channels[App.CurrentChannelId].Messages.Reverse())
                        {
                            adCheck--;
                            string header = null;
                            if (message.Value.Raw.Id == Session.RPC[App.CurrentChannelId].LastMessageId)
                                header = "NEW MESSAGES";
                            Messages.Items.Add(NewMessageContainer(message.Value.Raw, null, false, header));
                            if (adCheck == 0 && App.ShowAds)
                            {
                                Messages.Items.Add(NewMessageContainer(null, null, true, null));
                                adCheck = 5;
                            }
                        }
                    });

                Messages.Items.RemoveAt(0);

                PinnedMessages.Items.Clear();

                foreach (KeyValuePair<string, Message> message in App.CurrentGuild.Channels[App.CurrentChannelId].PinnedMessages.Reverse())
                {
                    adCheck--;
                    PinnedMessages.Items.Add(NewMessageContainer(message.Value.Raw, false, false, null));
                    if (adCheck == 0 && App.ShowAds)
                    {
                        PinnedMessages.Items.Add(NewMessageContainer(null, false, true, null));
                        adCheck = 5;
                    }
                }

                ChannelName.Text = "#" + App.CurrentGuild.Channels[App.CurrentChannelId].Raw.Name;

                if (App.CurrentGuild.Channels[App.CurrentChannelId].Raw.Topic != null)
                {
                    ChannelTopic.Text = App.CurrentGuild.Channels[App.CurrentChannelId].Raw.Topic;
                }
                else
                {
                    ChannelTopic.Text = "";
                }
                if (Session.Online)
                {
                    await DownloadChannelMessages();
                } else
                {
                    MessagesLoading.Visibility = Visibility.Collapsed;
                    if (Messages.Items.Count > 0)
                    {
                        NoMessageChached.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        NoMessageChached.Visibility = Visibility.Visible;
                    }
                }
            }
        }
        private async Task DownloadChannelMessages()
        {
            if (TextChannels.SelectedItem != null)
            {
                SendMessage.Visibility = Visibility.Visible;
                //MuteToggle.Tag = App.CurrentGuildId;
                //MuteToggle.IsChecked = Storage.MutedChannels.Contains(App.CurrentGuildId);
                //MuteToggle.Visibility = Visibility.Visible;
                Messages.Items.Clear();

                Storage.Cache.Guilds[(ServerList.SelectedItem as SimpleGuild).Id].Channels[App.CurrentChannelId].Messages.Clear();

                int adCheck = 5;

                Messages.Items.Add(new MessageControl()); //Necessary for no good reason

                IEnumerable<SharedModels.Message> messages = null;
                await Task.Run(async () =>
                {
                    messages = await Session.GetChannelMessages(App.CurrentChannelId);
                });

                foreach (SharedModels.Message message in messages)
                {
                    Storage.Cache.Guilds[(ServerList.SelectedItem as SimpleGuild).Id].Channels[App.CurrentChannelId].Messages.Add(message.Id, new Message(message));
                }

                //Normal messages
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        foreach (KeyValuePair<string, Message> message in Storage.Cache.Guilds[App.CurrentGuildId].Channels[App.CurrentChannelId].Messages.Reverse())
                        {
                            adCheck--;
                            Messages.Items.Add(NewMessageContainer(message.Value.Raw, null, false, null));
                            if (adCheck == 0 && App.ShowAds)
                            {
                                Messages.Items.Add(NewMessageContainer(null, null, true, null));
                                adCheck = 5;
                            }
                        }
                    });

                Messages.Items.RemoveAt(0);

                ChannelName.Text = "#" + Storage.Cache.Guilds[App.CurrentGuildId].Channels[App.CurrentChannelId].Raw.Name;

                if (Storage.Cache.Guilds[App.CurrentGuildId].Channels[App.CurrentChannelId].Raw.Topic != null)
                {
                    ChannelTopic.Text = Storage.Cache.Guilds[App.CurrentGuildId].Channels[App.CurrentChannelId].Raw.Topic;
                }
                else
                {
                    ChannelTopic.Text = "";
                }

                if (TextChannels.SelectedItem != null && Storage.Cache.Guilds[App.CurrentGuildId].Channels[App.CurrentChannelId].Messages != null)
                {
                    if (Storage.RecentMessages.ContainsKey(App.CurrentChannelId))
                    {
                        Storage.RecentMessages[App.CurrentChannelId] = (Messages.Items.Last() as MessageContainer)?.Message?.Id;
                    }
                    else if (Messages.Items.Count > 0)
                    {
                        var messageContainer = Messages.Items.Last() as MessageContainer;
                        if (messageContainer?.Message != null)
                        {
                            Storage.RecentMessages.Add(App.CurrentChannelId, (Messages.Items.Last() as MessageContainer)?.Message?.Id);
                        }
                    }
                    Storage.SaveMessages();
                }
                Storage.SaveCache();
                if (Messages.Items.Count > 0)
                    await Task.Run(() => Session.AckMessage(App.CurrentChannelId, Storage.Cache.Guilds[App.CurrentGuildId].Channels[App.CurrentChannelId].Raw.LastMessageId));
                MessagesLoading.Visibility = Visibility.Collapsed;
            }
        }

        private async Task DownloadChannelPinnedMessages()
        {
            //Pinned messages
            PinnedMessages.Items.Clear();
            if (App.CurrentGuildId != null)
            {
                IEnumerable<SharedModels.Message> pinnedmessages = await Session.GetChannelPinnedMessages(App.CurrentChannelId);
                Storage.Cache.Guilds[App.CurrentGuildId].Channels[App.CurrentChannelId].PinnedMessages.Clear();

                foreach (SharedModels.Message message in pinnedmessages)
                {
                    Storage.Cache.Guilds[App.CurrentGuildId].Channels[App.CurrentChannelId].PinnedMessages.Add(message.Id, new Message(message));
                }

                int adCheck = 5;

                foreach (KeyValuePair<string, Message> message in Storage.Cache.Guilds[App.CurrentGuildId].Channels[App.CurrentChannelId].PinnedMessages.Reverse())
                {
                    adCheck--;
                    PinnedMessages.Items.Add(NewMessageContainer(message.Value.Raw, false, false, null));
                    if (adCheck == 0 && App.ShowAds)
                    {
                        PinnedMessages.Items.Insert(1, NewMessageContainer(null, false, true, null));
                        adCheck = 5;
                    }
                }
            }
            else
            {
                IEnumerable<SharedModels.Message> pinnedmessages = await Session.GetChannelPinnedMessages(((DirectMessageChannels.SelectedItem as ListViewItem)?.Tag as DmCache)?.Raw.Id);
                Storage.Cache.DMs[((DirectMessageChannels.SelectedItem as ListViewItem)?.Tag as DmCache)?.Raw.Id].PinnedMessages.Clear();

                foreach (SharedModels.Message message in pinnedmessages)
                {
                    Storage.Cache.DMs[((DirectMessageChannels.SelectedItem as ListViewItem)?.Tag as DmCache)?.Raw.Id].PinnedMessages.Add(message.Id, new Message(message));
                }

                int adCheck = 5;

                foreach (KeyValuePair<string, Message> message in Storage.Cache.DMs[((DirectMessageChannels.SelectedItem as ListViewItem)?.Tag as DmCache)?.Raw.Id].PinnedMessages.Reverse())
                {
                    adCheck--;
                    PinnedMessages.Items.Add(NewMessageContainer(message.Value.Raw, false, false, null));
                    if (adCheck == 0 && App.ShowAds)
                    {
                        PinnedMessages.Items.Insert(1, NewMessageContainer(null, false, true, null));
                        adCheck = 5;
                    }
                }
            }
        }
        #endregion

        #region LoadDMChannel
        private async void LoadDmChannelMessages(object sender, SelectionChangedEventArgs e)
        {
            if(DirectMessageChannels.SelectedIndex == -1)
            {
                FriendsLVitem.IsSelected = true;
                friendPanel.Visibility = Visibility.Visible;
                SendMessage.Visibility = Visibility.Collapsed;
                Messages.Visibility = Visibility.Collapsed;
                headerButton.Visibility = Visibility.Collapsed;
                return;
            }
            else
            {
                FriendsLVitem.IsSelected = false;
                friendPanel.Visibility = Visibility.Collapsed;
                SendMessage.Visibility = Visibility.Visible;
                headerButton.Visibility = Visibility.Visible;
                Messages.Visibility = Visibility.Visible;
            }
            if (DirectMessageChannels.SelectedItem != null)
            {
                App.CurrentChannelId = (DirectMessageChannels.SelectedItem as SimpleChannel).Id;
                if (Session.Online)
                {
                    Session.Gateway.SubscribeToGuild(new string[] { App.CurrentChannelId });
                }
                UpdateTypingUI();
                if (Servers.DisplayMode == SplitViewDisplayMode.CompactOverlay || Servers.DisplayMode == SplitViewDisplayMode.Overlay)
                    Servers.IsPaneOpen = false;
                MessagesLoading.Visibility = Visibility.Visible;
                SendMessage.Visibility = Visibility.Visible;
                //MuteToggle.Visibility = Visibility.Collapsed;
                PinnedMessageToggle.Visibility = Visibility.Visible;

                Messages.Items.Clear();
                int adCheck = 5;

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    foreach (KeyValuePair<string, Message> message in Storage.Cache.DMs[(DirectMessageChannels.SelectedItem as SimpleChannel).Id].Messages.Reverse())
                    {
                        adCheck--;
                        Messages.Items.Add(NewMessageContainer(message.Value.Raw, null, false, null));
                        if (adCheck == 0 && App.ShowAds)
                        {
                            Messages.Items.Add(NewMessageContainer(null, null, true, null));
                            adCheck = 5;
                        }
                    }
                });

                ChannelName.Text = "@" + Storage.Cache.DMs[(DirectMessageChannels.SelectedItem as SimpleChannel).Id].Raw.Users.FirstOrDefault().Username;
                ChannelTopic.Text = "";

                if (Session.Online)
                {
                    await DownloadDmChannelMessages();
                } else
                {
                    MessagesLoading.Visibility = Visibility.Collapsed;
                    if (Messages.Items.Count > 0)
                    {
                        NoMessageChached.Visibility = Visibility.Collapsed;
                    } else
                    {
                        NoMessageChached.Visibility = Visibility.Visible;
                    }
                }
            }
        }
        private async Task DownloadDmChannelMessages()
        {
            SendMessage.Visibility = Visibility.Visible;
            //MuteToggle.Visibility = Visibility.Collapsed;

            Messages.Items.Clear();
            int adCheck = 5;

            IEnumerable<SharedModels.Message> messages = null;

            await Task.Run(async () => { messages = await Session.GetChannelMessages(App.CurrentChannelId); });

            if (Storage.Cache.DMs != null)
            {
                Storage.Cache.DMs[(DirectMessageChannels.SelectedItem as SimpleChannel).Id].Messages.Clear();
            }

            if (messages != null)
            {
                foreach (SharedModels.Message message in messages)
                {
                    Storage.Cache.DMs[(DirectMessageChannels.SelectedItem as SimpleChannel).Id].Messages.Add(message.Id, new Message(message));
                    Storage.SaveCache();
                }
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    Messages.Items.Add(new MessageControl()); //Necessary for no good reason

                if (Storage.Cache.DMs[(DirectMessageChannels.SelectedItem as SimpleChannel).Id].Messages != null)
                {
                    foreach (KeyValuePair<string, Message> message in Storage.Cache.DMs[(DirectMessageChannels.SelectedItem as SimpleChannel).Id].Messages.Reverse())
                    {
                        adCheck--;
                        Messages.Items.Add(NewMessageContainer(message.Value.Raw, null, false, null));
                        if (adCheck == 0 && App.ShowAds)
                        {
                            Messages.Items.Add(NewMessageContainer(null, null, true, null));
                            adCheck = 5;
                        }
                    }
                }

                Messages.Items.RemoveAt(0);
            });

            await Task.Run(() => Session.AckMessage(App.CurrentChannelId, Storage.Cache.DMs[App.CurrentChannelId].Raw.LastMessageId));
            if (DirectMessageChannels.SelectedItem != null && Storage.Cache.DMs[(DirectMessageChannels.SelectedItem as SimpleChannel).Id].Messages != null && Messages.Items.Count > 0)
            {
                if (Storage.RecentMessages.ContainsKey((DirectMessageChannels.SelectedItem as SimpleChannel).Id))
                {
                    Storage.RecentMessages[(DirectMessageChannels.SelectedItem as SimpleChannel).Id] = (Messages.Items.Last() as SharedModels.Message?)?.Id;
                }
                else
                {
                    Storage.RecentMessages.Add((DirectMessageChannels.SelectedItem as SimpleChannel).Id, (Messages.Items.Last() as SharedModels.Message?)?.Id);
                }
                Storage.SaveMessages();
            }
            MessagesLoading.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region General
        bool _onlyAllowOpeningPane = false;
        private void ToggleServerListFull(object sender, RoutedEventArgs e)
        {
            if (!Servers.IsPaneOpen && (Servers.DisplayMode == SplitViewDisplayMode.Overlay || Servers.DisplayMode == SplitViewDisplayMode.CompactOverlay))
                DarkenMessageArea.Begin();
            if (Servers.DisplayMode == SplitViewDisplayMode.CompactInline)
            {
                if (Servers.IsPaneOpen && !_onlyAllowOpeningPane)
                {
                    Servers.IsPaneOpen = false;
                    MessageArea.Margin = new Thickness(64, MessageArea.Margin.Top, MessageArea.Margin.Right, MessageArea.Margin.Bottom);
                }
                else
                {
                    Servers.IsPaneOpen = true;
                    MessageArea.Margin = new Thickness(320, MessageArea.Margin.Top, MessageArea.Margin.Right, MessageArea.Margin.Bottom);
                }
            }
            else if(Servers.IsPaneOpen && !_onlyAllowOpeningPane )
                Servers.IsPaneOpen = false;
            else
                Servers.IsPaneOpen = true;

            _onlyAllowOpeningPane = false;
        }
        private void Refresh(object sender, RoutedEventArgs e)
        {
            Login();
        }
        private void TogglePeopleShow(object sender, RoutedEventArgs e)
        {
            if (!Members.IsPaneOpen && (Members.DisplayMode == SplitViewDisplayMode.Overlay || Members.DisplayMode == SplitViewDisplayMode.CompactOverlay))
                DarkenMessageArea.Begin();
            if (Members.DisplayMode == SplitViewDisplayMode.Inline)
            {
                if (Members.IsPaneOpen)
                {
                    Members.IsPaneOpen = false;
                    MessageArea.Margin = new Thickness(MessageArea.Margin.Left, MessageArea.Margin.Top, 0, MessageArea.Margin.Bottom);
                }
                else
                {
                    Members.IsPaneOpen = true;
                    MessageArea.Margin = new Thickness(MessageArea.Margin.Left, MessageArea.Margin.Top, 240, MessageArea.Margin.Bottom);
                }
            }
            else
            {
                Members.IsPaneOpen = !Members.IsPaneOpen;
            }
        }

        private async void TogglePinnedShow(object sender, RoutedEventArgs e)
        {
            PinnedMessagesLoading.Visibility = Visibility.Visible;
            await DownloadChannelPinnedMessages();
            PinnedMessagesLoading.Visibility = Visibility.Collapsed;
        }

        private async void LoadMoreMessages(object sender, TappedRoutedEventArgs e)
        {
            if (Messages.Items.Count > 0)
            {
                IEnumerable<SharedModels.Message> newMessages = null;
                if (!(Messages.Items[0] as MessageContainer).IsAdvert)
                {
                    newMessages = await Session.GetChannelMessagesBefore(App.CurrentChannelId, (Messages.Items[0] as MessageContainer).Message.Value.Id);
                } else
                {
                    newMessages = await Session.GetChannelMessagesBefore(App.CurrentChannelId, (Messages.Items[1] as MessageContainer).Message.Value.Id);
                }

                int adCheck = 5;

                foreach (SharedModels.Message msg in newMessages)
                {
                    adCheck--;
                    Messages.Items.Insert(0, NewMessageContainer(msg, null, false, null));
                    if (adCheck == 0 && App.ShowAds)
                    {
                        Messages.Items.Insert(0, NewMessageContainer(null, null, true, null));
                        adCheck = 5;
                    }
                }
            }
        }

        private void OpenWhatsNew(object sender, RoutedEventArgs e)
        {
            SubFrameNavigator(typeof(SubPages.About));
        }
        private void OpenIaPs(object sender, RoutedEventArgs e)
        {
            SubFrameNavigator(typeof(SubPages.InAppPurchases));
        }

        #endregion

        #region ChannelSettings
        private void OpenChannelSettings(object sender, RoutedEventArgs e)
        {
           
            App.NavigateToChannelEdit((sender as Button).Tag.ToString());
        }
        #endregion
        public static bool SelectChannel = false;
        public static string SelectChannelId = "";
        public static string SelectGuildId = "";
        string _settingsPaneId;

        private void SP_PaneClosing(SplitView sender, SplitViewPaneClosingEventArgs args)
        {
            LightenMessageArea.Begin();
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            OpenChannelSettings(null,null);
        }

        private void Messages_RefreshRequested(object sender, EventArgs e)
        {
            LoadMoreMessages(null,null);
        }

        private void ShowUserDetails(string id)
        {
            SubFrameNavigator(typeof(SubPages.UserProfile), id);
       //    await new MessageDialog("Sorry, but this feature hasn't yet been added into Discord UWP, it will be available in the next update. The fact that we didn't add it means you got access to the rest of the app sooner ;)", "Coming soon™")
         //       .ShowAsync();
        }
        private async void MessageControl_OnLinkClicked(object sender, MarkdownTextBlock.LinkClickedEventArgs e)
        {
            if (e.Link.StartsWith("#"))
            {
                string val = e.Link.Remove(0, 1);
                foreach (SimpleChannel item in TextChannels.Items)
                {
                    if (item.Id == val)
                    {
                        TextChannels.SelectedItem = item;
                    }
                }
            }
            else if (e.Link.StartsWith("@!"))
            {
                string val = e.Link.Remove(0, 2);
                ShowUserDetails(val);
            }
            else if (e.Link.StartsWith("@&"))
            {
                string val = e.Link.Remove(0, 2);
                //TODO Fix this shit
                MembersListView.ScrollIntoView(memberscvs.FirstOrDefault(x => x.Value.MemberDisplayedRole.Id == val));
                if (!Members.IsPaneOpen)
                {
                    if (ResponsiveUI_VisualStates.CurrentState != Large)
                        DarkenMessageArea.Begin();
                    Members.IsPaneOpen = true;
                }
            }
            else if (e.Link.StartsWith("@"))
            {
                string val = e.Link.Remove(0, 1);
                ShowUserDetails(val);
            }
            else
            {
                await Launcher.LaunchUriAsync(new Uri(e.Link));
            }
        }

        private async void Upvote_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://aka.ms/Wp1zo6"));
        }

        private bool LocalStatusChangeEnabled = false;
        private void UserStatus_Checked(object sender, RoutedEventArgs e)
        {
            if (Playing != null) /*Called pre-full-initialization*/
            {
                Playing.IsEnabled = true;
                if (UserStatusOnline.IsChecked == true && LocalStatusChangeEnabled)
                    Session.ChangeUserSettings("online");

                else if (UserStatusIdle.IsChecked == true && LocalStatusChangeEnabled)
                    Session.ChangeUserSettings("idle");

                else if (UserStatusDND.IsChecked == true && LocalStatusChangeEnabled)
                    Session.ChangeUserSettings("dnd");

                else if (UserStatusInvisible.IsChecked == true && LocalStatusChangeEnabled)
                {
                    Session.ChangeUserSettings("invisible");
                    Playing.IsEnabled = false;
                }
            }
            LocalStatusChangeEnabled = true;
        }

        private void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {
            UserFlyoutBtn.Flyout.Hide();
            SubFrameNavigator(typeof(SubPages.Settings));
        }

        private void SubFrameNavigator(Type page, object args = null)
        {
            /*maybe enable this blur effect later, depending on the GPU */
            if (Storage.Settings.ExpensiveRender)
            {
                content.Blur(2, 600).Start();
            }
            SubFrame.Visibility = Visibility.Visible;
            SubFrame.Navigate(page, args);
        }
        private void SubpageClosed(object sender, EventArgs e)
        {
            /*maybe enable this blur effect later, depending on the GPU */
            if (Storage.Settings.ExpensiveRender)
            {
                content.Blur(0, 600).Start();
            } else
            {
                content.Blur(0, 0).Start();
            }
        }

        #region OldCode
        //private void LockChannelsToggled(object sender, RoutedEventArgs e)
        //{
        //    if ((sender as ToggleSwitch).IsOn)
        //    {
        //             AutoHideChannels.IsEnabled = false;
        //             AutoHideChannels.IsOn = false;
        //    }
        //    else
        //    {
        //             AutoHideChannels.IsEnabled = true;
        //             AutoHideChannels.IsOn = Storage.settings.AutoHideChannels;
        //    }
        //}

        //private void Scrolldown(object sender, SizeChangedEventArgs e)
        //{
        //    if (e.PreviousSize.Height < 10)
        //    {
        //          MessageScroller.ChangeView(0.0f, MessageScroller.ScrollableHeight, 1f);
        //    }
        //}
        #endregion

        private void OpenUserSettings(object sender, RoutedEventArgs e)
        {
            SubFrameNavigator(typeof(SubPages.Settings));
        }

        private void Playing_OnLostFocus(object sender, RoutedEventArgs e)
        {
            Session.ChangeCurrentGame(Playing.Text);
        }

        private void ServerName_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!App.CurrentGuildIsDM)
            {
                App.NavigateToGuildEdit(App.CurrentGuildId);
            }
        }

        private void AddServer(object sender, RoutedEventArgs e)
        {
            SubFrameNavigator(typeof(SubPages.AddServer));
        }

        bool iscompact = false;
        private async void CompactOverlayToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!iscompact)
            {
                ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                compactOptions.CustomSize = new Size(300, 512);
                await Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
                commandBar.Visibility = Visibility.Collapsed;
                ChannelInfo.Visibility = Visibility.Collapsed;
                Servers.Visibility = Visibility.Collapsed;
                Members.Visibility = Visibility.Collapsed;
                iscompact = true;
            }
            else
            {
                await Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                commandBar.Visibility = Visibility.Visible;
                ChannelInfo.Visibility = Visibility.Visible;
                Servers.Visibility = Visibility.Visible;
                Members.Visibility = Visibility.Visible;
                iscompact = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SubFrameNavigator(typeof(SubPages.ChannelTopic), TextChannels.SelectedItem as SimpleChannel);
        }

        private void AddChannelButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SubFrameNavigator(typeof(SubPages.CreateChannel), null);
        }

        private void MessageBox1_OpenAdvanced(object sender, RoutedEventArgs e)
        {
            SubFrameNavigator(typeof(SubPages.ExtendedMessageEditor), MessageBox1.Text);
        }

        private void HideBadge_Completed(object sender, object e)
        {

        }

        private void FriendsLVitem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DirectMessageChannels.SelectedIndex = -1;
        }
    }
}