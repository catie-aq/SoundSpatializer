﻿using System;
using System.Collections.Generic;

using System.Text;
using TeamDev.Redis.Interface;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Net.Sockets;

using UnityEngine;

namespace TeamDev.Redis.LanguageItems
{
  public class LanguageMessaging : ILanguageItem, IComplexItem
  {
    internal volatile string _name;
    internal volatile RedisDataAccessProvider _provider;

    private volatile bool _wasbalancingcalls = false;
    private volatile Thread _originalThread = Thread.CurrentThread;
    private volatile Thread _readingThread = null;

    public bool _shouldRun = true;

    
    public int Publish(string channel, string message)
    {
      return _provider.ReadInt(_provider.SendCommand(RedisCommand.PUBLISH, channel, message));
    }

    
    public void Subscribe(params string[] channels)
    {
      _provider.SendCommand(RedisCommand.SUBSCRIBE, channels);

      if (_readingThread == null || _readingThread.ThreadState != ThreadState.Running)
      {
        _wasbalancingcalls = _provider.Configuration.LogUnbalancedCommands;

        _readingThread = new Thread(new ParameterizedThreadStart(ChannelsReadingThread));
        _readingThread.IsBackground = true;
        _readingThread.Priority = System.Threading.ThreadPriority.Highest;
        _provider.ShareConnectionWithThread(_readingThread.ManagedThreadId);

        _readingThread.Start(new ProviderState() { Provider = _provider, Stream = _provider.GetBStream() });

        _provider.RemoveConnectionFromThread(Thread.CurrentThread.ManagedThreadId);
      }
    }

    public void Unsubscribe(params string[] channels)
    {
      if(_readingThread != null && (_readingThread.ThreadState == ThreadState.Running || _readingThread.ThreadState == ThreadState.Background)) {
        _provider.SendCommand(RedisCommand.UNSUBSCRIBE, channels);
      }
    }

    void ChannelsReadingThread(object state)
    {
      try {
        var provider = state as ProviderState;
        if (provider != null)
        {
          var stream = provider.Stream;
          while (true)
          {
            //_provider.Connect();
            while (!stream.DataAvailable) {
              Thread.Sleep(this._provider.Configuration.ReceiveDelayms);
            }
            var operation = _provider.ReadString();
          
            switch (operation)
            {
              case "subscribe":
                var channel = _provider.ReadString();
                var clients = _provider.ReadInt();
                provider.Provider.RaiseChannelSubscribedEvent(channel);
                break;
              case "unsubscribe":
                var uchannel = _provider.ReadString();
                var uclients = _provider.ReadInt();
                provider.Provider.RaiseChannelUnsubscribedEvent(uchannel);

                if (uclients == 0) return;
                break;
              case "message":
                // Reverted to default implementation for strings
                var mchannel = _provider.ReadString();
                var message = _provider.ReadString();
                provider.Provider.RaiseMessageReceivedEvent(mchannel, message);
             
                // Modified by nclsp, 14:26 - 21/06/2018 - Reading message as binary instead of string.
                //var mchannel = provider.Provider.ReadString();               
                //var bmessage = provider.Provider.ReadData();
                //provider.Provider.RaiseBinaryMessageReceivedEvent(mchannel, bmessage);
                break;
              default:
                break;
            }
          }
        }
        else {
          Debug.Log("Provider null");
        }
      }
      catch (Exception e) {
        Debug.Log(e.StackTrace + e.Message);
      }
    }

    void ILanguageItem.Configure(string name, RedisDataAccessProvider provider)
    {
      _name = name;
      _provider = provider;
    }

    string IComplexItem.KeyName
    {
      get { return _name; }
    }

    RedisDataAccessProvider IComplexItem.Provider
    {
      get { return _provider; }
    }
  }

  public class ProviderState
  {
    public RedisDataAccessProvider Provider { get; set; }
    public NetworkStream Stream { get; set; }
  }
}
