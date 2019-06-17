﻿using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quarrel.Helpers;
using Quarrel.Models.Bindables;
using Quarrel.Messages.Gateway;
using Quarrel.Services;
using Quarrel.Services.Cache;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarSymbols;
using UICompositionAnimations.Helpers;

namespace Quarrel.ViewModels
{
    public class GuildViewModel
    {
        public GuildViewModel()
        {
            Messenger.Default.Register<GatewayReadyMessage>(this, async _ =>
            {
                await DispatcherHelper.RunAsync(() =>
                {
                    var itemList = ServicesManager.Cache.Runtime.TryGetValue<List<BindableGuild>>(Constants.Cache.Keys.GuildList);
                    foreach (var item in itemList)
                    {
                        Source.Add(item);
                    }
                });
            });
        }

        public ObservableCollection<BindableGuild> Source { get; private set; } = new ObservableCollection<BindableGuild>();
    }
}