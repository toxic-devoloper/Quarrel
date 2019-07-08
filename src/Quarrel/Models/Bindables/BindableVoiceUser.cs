﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordAPI.Models;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using JetBrains.Annotations;
using Quarrel.Messages.Gateway;
using Quarrel.Models.Bindables.Abstract;
using Quarrel.Services.Users;

namespace Quarrel.Models.Bindables
{
    public class BindableVoiceUser : BindableModelBase<VoiceState>, IEquatable<BindableVoiceUser>, IComparable<BindableVoiceUser>
    {
        public ICurrentUsersService UserService => SimpleIoc.Default.GetInstance<ICurrentUsersService>();

        public BindableUser User => UserService?.Users[Model.UserId]?[Model.UserId];

        public bool ShowDeaf => Model.SelfDeaf || Model.ServerDeaf;
        public bool ServerDeaf => ShowDeaf && Model.ServerDeaf;
        public bool LocalDeaf => ShowDeaf && !Model.ServerDeaf;

        public bool ShowMute => Model.SelfMute || Model.ServerMute;
        public bool ServerMute => ShowMute && Model.ServerMute;
        public bool LocalMute => ShowMute && !Model.ServerMute;

        public BindableVoiceUser([NotNull] VoiceState model) : base(model)
        {
            Messenger.Default.Register<GatewayVoiceStateUpdateMessage>(this, async e =>
                {
                    await DispatcherHelper.RunAsync(() =>
                    {
                        if (e.VoiceState.UserId == Model.UserId)
                        {
                            if (e.VoiceState.SelfDeaf != Model.SelfDeaf)
                            {
                                Model.SelfDeaf = e.VoiceState.SelfDeaf;
                                UpateProperties();
                            }

                            if (e.VoiceState.SelfMute != Model.SelfMute)
                            {
                                Model.SelfMute = e.VoiceState.SelfMute;
                                UpateProperties();
                            }

                            if (e.VoiceState.ServerDeaf != Model.ServerDeaf)
                            {
                                Model.ServerDeaf = e.VoiceState.ServerDeaf;
                                UpateProperties();
                            }

                            if (e.VoiceState.ServerMute != Model.ServerMute)
                            {
                                Model.ServerMute = e.VoiceState.ServerMute;
                                UpateProperties();
                            }
                        }
                    });
                }
            );
        }

        private void UpateProperties()
        {
            RaisePropertyChanged(nameof(ShowDeaf));
            RaisePropertyChanged(nameof(ServerDeaf));
            RaisePropertyChanged(nameof(LocalDeaf));
            RaisePropertyChanged(nameof(ShowMute));
            RaisePropertyChanged(nameof(ServerMute));
            RaisePropertyChanged(nameof(LocalMute));
        }

        public bool Equals(BindableVoiceUser other)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(BindableVoiceUser other)
        {
            throw new NotImplementedException();
        }
    }
}
