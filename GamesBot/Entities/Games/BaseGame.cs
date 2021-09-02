using System;
using System.Threading.Tasks;

namespace Games.Entities.Games
{
    public abstract class BaseGame<TResult> : IDisposable
    {
        public BaseGameType Type { get; }

        public BaseGame(BaseGameType type)
        {
            this.Type = type;
        }

        public virtual Task<TResult> GetResultAsync()
        {
            return Task.FromResult(default(TResult));
        }

        public virtual void Dispose()
        {

        }
    }

    public enum BaseGameType
    {
        TicTacToe,
        Hangman
    }
}
