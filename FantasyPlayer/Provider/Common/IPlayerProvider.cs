using System.Threading.Tasks;
using FantasyPlayer.Interface;
using FantasyPlayer.Interfaces;

namespace FantasyPlayer.Provider.Common
{
    using System;
    using Microsoft.Extensions.Hosting;

    public interface IPlayerProvider : IHostedService, IDisposable
    {
        public PlayerStateStruct PlayerState { get; set; }
        public Task<IPlayerProvider> Initialize();
        public string Key { get; }
        public string Name { get; }
        public bool Initialized { get; }
        public void Update();
        public void StartAuth();
        public void RetryAuth();
        public void ReAuth();

        public void SwapRepeatState();
        public void SetPauseOrPlay(bool play);
        public void SetSkip(bool forward);
        public void SetShuffle(bool value);
        public void SetVolume(int volume);
    }
}