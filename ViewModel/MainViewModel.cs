using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProceduralTrackGeneration.ViewModel
{
    public static class MultithreadedRandom
    {
        static int seed = Environment.TickCount;

        static readonly ThreadLocal<Random> random =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

        public static int Rand()
        {
            return random.Value.Next();
        }
    }

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {

            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}

            Tracks = Enumerable.Range(1, 9).Select(i => new TrackViewModel(200, 160, 20)).ToList();
            RenewTracks();
        }

        private IList<TrackViewModel> tracks;
        public IList<TrackViewModel> Tracks
        {
            get => tracks;
            private set => Set(ref tracks, value);
        }

        /// <summary>
        /// Renew all tracks asynchronously.
        /// </summary>
        private async void RenewTracks()
        {
            int i = Environment.TickCount;
            foreach (var track in tracks)
            {
                await Task.Run(() =>
                {
                    Random rnd = new Random(i++);
                    track.GenerateTrack(rnd);
                });
            }
        }

        public ICommand RenewTracksCommand => new RelayCommand(RenewTracks);
    }
}